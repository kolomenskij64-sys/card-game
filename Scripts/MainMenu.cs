using Godot;
using System;

public partial class MainMenu : Control
{
	[Export] public PackedScene GameScene;

	private Button _playButton;
	private Button _quitButton;
	private Label _highScoreLabel;
	private bool _isInitialized = false;

	public override void _EnterTree()
	{
		GD.Print("=== ГЛАВНОЕ МЕНЮ ПОКАЗАНО ===");

		// Находим ноды (только если ещё не найдены)
		if (_playButton == null)
			_playButton = GetNodeOrNull<Button>("VBoxContainer/Playbutton");
		if (_quitButton == null)
			_quitButton = GetNodeOrNull<Button>("VBoxContainer/Quitbutton");
		if (_highScoreLabel == null)
			_highScoreLabel = GetNodeOrNull<Label>("VBoxContainer/Hightscorelabel");

		// Подключаем сигналы (только один раз)
		if (_playButton != null && !_playButton.IsConnected("pressed", Callable.From(OnPlayButtonPressed)))
		{
			_playButton.Pressed += OnPlayButtonPressed;
			GD.Print("✅ Кнопка 'Играть' подключена");
		}

		if (_quitButton != null && !_quitButton.IsConnected("pressed", Callable.From(OnQuitButtonPressed)))
		{
			_quitButton.Pressed += OnQuitButtonPressed;
			GD.Print("✅ Кнопка 'Выйти' подключена");
		}

		// Загружаем рекорд с задержкой, чтобы DatabaseAPI успел инициализироваться
		CallDeferred(nameof(LoadHighScore));
	}

	private async void LoadHighScore()
	{
		if (_highScoreLabel == null) return;

		// Ждём пока DatabaseAPI инициализируется
		int retries = 0;
		while (DatabaseAPI.Instance == null && retries < 10)
		{
			GD.Print($"[MainMenu] Ждём DatabaseAPI... попытка {retries + 1}");
			await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
			retries++;
		}

		if (DatabaseAPI.Instance == null)
		{
			GD.PrintErr("[MainMenu] DatabaseAPI.Instance так и не инициализировался!");
			_highScoreLabel.Text = "Рекорд: -";
			return;
		}

		try
		{
			int highScore = await DatabaseAPI.Instance.GetHighScoreAsync("Player1");
			_highScoreLabel.Text = $"Рекорд: {highScore}";
			GD.Print($"🏆 Рекорд загружен: {highScore}");
		}
		catch (Exception ex)
		{
			GD.PrintErr("Ошибка загрузки рекорда: " + ex.Message);
			_highScoreLabel.Text = "Рекорд: -";
		}
	}

	public void OnPlayButtonPressed()
	{
		GD.Print("🎮 Нажата кнопка 'ИГРАТЬ'");

		if (GameScene != null)
		{
			GD.Print("Переход на GameScene...");
			GetTree().ChangeSceneToPacked(GameScene);
		}
		else
		{
			GD.PrintErr("⚠️ GameScene не задан в Инспекторе! Пробуем загрузить напрямую...");
			GetTree().ChangeSceneToFile("res://Scenes/Game.tscn");
		}
	}

	public void OnQuitButtonPressed()
	{
		GD.Print("🚪 Нажата кнопка 'ВЫЙТИ'");
		GetTree().Quit();
	}
}
