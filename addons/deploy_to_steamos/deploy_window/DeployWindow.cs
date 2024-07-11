using System.Collections.Generic;
using Godot;

namespace Laura.DeployToSteamOS;

[Tool]
public partial class DeployWindow : Window
{
	public enum DeployStep
	{
		Init,
		Building,
		PrepareUpload,
		Uploading,
		CreateShortcut,
		Done
	}
	public DeployStep CurrentStep = DeployStep.Init;

	public enum StepProgress
	{
		Queued,
		Running,
		Succeeded,
		Failed
	}
	public StepProgress CurrentProgress = StepProgress.Queued;

	private string _localPath;
	private string _gameId;
	private SteamOSDevkitManager.Device _device;
	private SteamOSDevkitManager.PrepareUploadResult _prepareUploadResult;
	private SteamOSDevkitManager.CreateShortcutResult _createShortcutResult;

	private Dictionary<StepProgress, Color> _colorsBright = new()
	{
		{ StepProgress.Queued, new Color("#6a6a6a") },
		{ StepProgress.Running, new Color("#ffffff") },
		{ StepProgress.Succeeded, new Color("#00f294") },
		{ StepProgress.Failed, new Color("#ff4245") },
	};
	private Dictionary<StepProgress, Color> _colorsDim = new()
	{
		{ StepProgress.Queued, new Color("#1d1d1d") },
		{ StepProgress.Running, new Color("#222222") },
		{ StepProgress.Succeeded, new Color("#13241d") },
		{ StepProgress.Failed, new Color("#241313") },
	};

	[ExportGroup("References")]
	[Export] private VBoxContainer _buildingContainer;
	[Export] private VBoxContainer _prepareUploadContainer;
	[Export] private VBoxContainer _uploadingContainer;
	[Export] private VBoxContainer _createShortcutContainer;

	public override void _Process(double delta)
	{
		if (CurrentStep != DeployStep.Done)
		{
			var container = CurrentStep switch
			{
				DeployStep.Building => _buildingContainer,
				DeployStep.PrepareUpload => _prepareUploadContainer,
				DeployStep.Uploading => _uploadingContainer,
				DeployStep.CreateShortcut => _createShortcutContainer,
				_ => null
			};
			if (container != null)
			{
				var progressBar = container.GetNode<ProgressBar>("Progressbar");
				if (progressBar.Value < 95f)
				{
					var shouldDoubleSpeed = CurrentStep is DeployStep.PrepareUpload or DeployStep.CreateShortcut;
					progressBar.Value += (float)delta * (shouldDoubleSpeed ? 2f : 1f);
				}
			}
		}
	}

	public async void Deploy(SteamOSDevkitManager.Device device)
	{
		if (device == null)
		{
			GD.PrintErr("[DeployToSteamOS] Device is not available.");
			return;
		}

		_device = device;
		_prepareUploadResult = null;
		_createShortcutResult = null;
		ResetUI();

		Title = $"Deploying to {_device.DisplayName} ({_device.Login}@{_device.IPAdress})";
		Show();
		
		await DeployInit();
		await DeployBuild(Callable.From((Variant log) => AddToConsole(DeployStep.Building, log.AsString())));
		await DeployPrepareUpload(Callable.From((Variant log) => AddToConsole(DeployStep.PrepareUpload, log.AsString())));
		await DeployUpload(Callable.From((Variant log) => AddToConsole(DeployStep.Uploading, log.AsString())));
		await DeployCreateShortcut(Callable.From((Variant log) => AddToConsole(DeployStep.CreateShortcut, log.AsString())));
		
		CurrentStep = DeployStep.Done;
		CurrentProgress = StepProgress.Succeeded;
		UpdateUI();
	}

	public void Close()
	{
		if (CurrentStep != DeployStep.Init && CurrentStep != DeployStep.Done) return;
		Hide();
	}

	private void UpdateUI()
	{
		UpdateContainer(CurrentStep, CurrentProgress);
		if (CurrentStep == DeployStep.Done)
		{
			SetConsoleVisibility(DeployStep.Building, false, false);
			SetConsoleVisibility(DeployStep.PrepareUpload, false, false);
			SetConsoleVisibility(DeployStep.Uploading, false, false);
			SetConsoleVisibility(DeployStep.CreateShortcut, false, false);
		}
	}

	private void ResetUI()
	{
		// Clear Consoles
		List<Node> consoleNodes = new();
		consoleNodes.AddRange(_buildingContainer.GetNode<VBoxContainer>("ConsoleContainer/ScrollContainer/VBoxContainer").GetChildren());
		consoleNodes.AddRange(_prepareUploadContainer.GetNode<VBoxContainer>("ConsoleContainer/ScrollContainer/VBoxContainer").GetChildren());
		consoleNodes.AddRange(_uploadingContainer.GetNode<VBoxContainer>("ConsoleContainer/ScrollContainer/VBoxContainer").GetChildren());
		consoleNodes.AddRange(_createShortcutContainer.GetNode<VBoxContainer>("ConsoleContainer/ScrollContainer/VBoxContainer").GetChildren());
		
		foreach (var consoleNode in consoleNodes)
		{
			consoleNode.QueueFree();
		}
		
		// Clear States
		UpdateContainer(DeployStep.Building, StepProgress.Queued);
		UpdateContainer(DeployStep.PrepareUpload, StepProgress.Queued);
		UpdateContainer(DeployStep.Uploading, StepProgress.Queued);
		UpdateContainer(DeployStep.CreateShortcut, StepProgress.Queued);

		CurrentStep = DeployStep.Init;
		CurrentProgress = StepProgress.Queued;
	}

	private void UpdateContainer(DeployStep step, StepProgress progress)
	{
		var container = step switch
		{
			DeployStep.Building => _buildingContainer,
			DeployStep.PrepareUpload => _prepareUploadContainer,
			DeployStep.Uploading => _uploadingContainer,
			DeployStep.CreateShortcut => _createShortcutContainer,
			_ => null
		};
		if (container == null) return;

		var headerContainer = container.GetNode<PanelContainer>("HeaderContainer");
		var label = headerContainer.GetNode<Label>("HBoxContainer/Label");
		var toggleConsoleButton = headerContainer.GetNode<Button>("HBoxContainer/ToggleConsoleButton");
		var progressBar = container.GetNode<ProgressBar>("Progressbar");
		
		label.AddThemeColorOverride("font_color", _colorsBright[progress]);
		
		headerContainer.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = _colorsDim[progress],
			ContentMarginTop = 10,
			ContentMarginBottom = 10,
			ContentMarginLeft = 10,
			ContentMarginRight = 10,
		});
		
		progressBar.AddThemeStyleboxOverride("fill", new StyleBoxFlat
		{
			BgColor = _colorsBright[progress],
		});
		progressBar.Value = progress switch
		{
			StepProgress.Queued => 0,
			StepProgress.Running => 0,
			StepProgress.Succeeded => 100,
			StepProgress.Failed => 100,
			_ => progressBar.Value
		};

		SetConsoleVisibility(step, progress == StepProgress.Running, false);

		toggleConsoleButton.Visible = progress is StepProgress.Succeeded or StepProgress.Failed;
		toggleConsoleButton.Disabled = progress is not StepProgress.Succeeded and not StepProgress.Failed;
	}

	private void AddToConsole(DeployStep step, string text)
	{
		var consoleContainer = step switch
		{
			DeployStep.Building => _buildingContainer.GetNode<PanelContainer>("ConsoleContainer"),
			DeployStep.PrepareUpload => _prepareUploadContainer.GetNode<PanelContainer>("ConsoleContainer"),
			DeployStep.Uploading => _uploadingContainer.GetNode<PanelContainer>("ConsoleContainer"),
			DeployStep.CreateShortcut => _createShortcutContainer.GetNode<PanelContainer>("ConsoleContainer"),
			_ => null
		};
		if (consoleContainer == null) return;

		var consoleScrollContainer = consoleContainer.GetNode<ScrollContainer>("ScrollContainer");
		var consoleVBoxContainer = consoleScrollContainer.GetNode<VBoxContainer>("VBoxContainer");

		// Create new Label
		var newLabel = new Label { Text = text };
		newLabel.AddThemeFontSizeOverride("font_size", 12);
		newLabel.AutowrapMode = TextServer.AutowrapMode.Word;
		newLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		
		consoleVBoxContainer.AddChild(newLabel);
		consoleScrollContainer.ScrollVertical = (int)consoleScrollContainer.GetVScrollBar().MaxValue;
	}

	private void SetConsoleVisibility(DeployStep step, bool shouldOpen, bool closeOthers)
	{
		var consoleContainer = step switch
		{
			DeployStep.Building => _buildingContainer.GetNode<PanelContainer>("ConsoleContainer"),
			DeployStep.PrepareUpload => _prepareUploadContainer.GetNode<PanelContainer>("ConsoleContainer"),
			DeployStep.Uploading => _uploadingContainer.GetNode<PanelContainer>("ConsoleContainer"),
			DeployStep.CreateShortcut => _createShortcutContainer.GetNode<PanelContainer>("ConsoleContainer"),
			_ => null
		};
		if (consoleContainer == null) return;

		// Open Container
		if (shouldOpen)
		{
			// Close all other open containers if not running anymore
			if (closeOthers)
			{
				var consoleContainers = new List<PanelContainer>
				{
					_buildingContainer.GetNode<PanelContainer>("ConsoleContainer"),
					_prepareUploadContainer.GetNode<PanelContainer>("ConsoleContainer"),
					_uploadingContainer.GetNode<PanelContainer>("ConsoleContainer"),
					_createShortcutContainer.GetNode<PanelContainer>("ConsoleContainer")
				};
				foreach (var container in consoleContainers)
				{
					container.Visible = false;
					container.GetParent<VBoxContainer>().SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
				}
			}

			// Open container
			consoleContainer.Visible = true;
			consoleContainer.GetParent<VBoxContainer>().SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		}
		else
		{
			consoleContainer.Visible = false;
			consoleContainer.GetParent<VBoxContainer>().SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
		}
	}

	private void ToggleConsoleVisibility(DeployStep step)
	{
		var consoleContainer = step switch
		{
			DeployStep.Building => _buildingContainer.GetNode<PanelContainer>("ConsoleContainer"),
			DeployStep.PrepareUpload => _prepareUploadContainer.GetNode<PanelContainer>("ConsoleContainer"),
			DeployStep.Uploading => _uploadingContainer.GetNode<PanelContainer>("ConsoleContainer"),
			DeployStep.CreateShortcut => _createShortcutContainer.GetNode<PanelContainer>("ConsoleContainer"),
			_ => null
		};
		if (consoleContainer == null) return;
		
		SetConsoleVisibility(step, !consoleContainer.Visible, CurrentStep == DeployStep.Done);
	}

	private void UpdateUIToFail()
	{
		CurrentProgress = StepProgress.Failed;
		UpdateUI();
		CurrentStep = DeployStep.Done;
		CurrentProgress = StepProgress.Queued;
		UpdateUI();
	}

	public void OnBuildingConsolePressed()
	{
		ToggleConsoleVisibility(DeployStep.Building);
	}
	public void OnPrepareUploadConsolePressed()
	{
		ToggleConsoleVisibility(DeployStep.PrepareUpload);
	}
	public void OnUploadingConsolePressed()
	{
		ToggleConsoleVisibility(DeployStep.Uploading);
	}
	public void OnCreatingShortcutConsolePressed()
	{
		ToggleConsoleVisibility(DeployStep.CreateShortcut);
	}
}
