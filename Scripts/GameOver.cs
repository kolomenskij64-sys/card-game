using Godot;
using System;
using System.Threading.Tasks;  

public partial class GameOver : Control
{
	[Export] public Label ResultLabel;
	[Export] public Label FinalScoreLabel;
	[Export] public Label HighScoreLabel;
	[Export] public Button RestartButton;
	[Export] public Button MenuButton;
	[Export] public Button QuitButton;

	private int _finalScore;
	private int _highScore;

	public override void _Ready()
	{
		GD.Print("[GameOver] GameOver загружен");
		
		// Получаем данные из GameManager
		if (GameManager.Instance != null)
		{
			_finalScore = GameManager.Instance.Score;
			GD.Print($"[GameOver] Финальный счёт: {_finalScore}");
		}
		else
		{
			_finalScore = GameOverData.FinalScore;
			GD.Print($"[GameOver] Финальный счёт из GameOverData: {_finalScore}");
		}
		
		InitializeAsync();
	}

	private async void InitializeAsync()
	{
		GD.Print("[GameOver] Начинаем инициализацию...");
		_highScore = await LoadHighScoreAsync();

		if (ResultLabel != null)
		{
			if (_finalScore > _highScore)
			{
				ResultLabel.Text = "🏆 НОВЫЙ РЕКОРД!";
				await SaveHighScoreAsync(_finalScore);
				_highScore = _finalScore;
			}
			else if (GameOverData.IsVictory)
			{
				ResultLabel.Text = " ПОЛНАЯ ПОБЕДА!";
			}
			else
			{
				ResultLabel.Text = "💀 ПОРАЖЕНИЕ";
			}
		}

		UpdateUI();
		ConnectButtons();
	}

	private async Task<int> LoadHighScoreAsync()
	{
		GD.Print("[GameOver] Начинаем загрузку рекорда...");
		
		if (DatabaseAPI.Instance != null)
		{
			GD.Print("[GameOver] DatabaseAPI.Instance найден, пытаемся загрузить из БД...");
			try
			{
				int score = await DatabaseAPI.Instance.GetHighScoreAsync("Player1");
				GD.Print($"[GameOver] ✅ Рекорд из БД: {score}");
				return score;
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[GameOver] Ошибка загрузки из БД: {ex.Message}");
			}
		}
		else
		{
			GD.PrintErr("[GameOver] DatabaseAPI.Instance = null!");
		}
		
		return 0;
	}

	private async Task SaveHighScoreAsync(int score)
	{
		if (DatabaseAPI.Instance != null)
		{
			try
			{
				await DatabaseAPI.Instance.UpdatePlayerScoreAsync("Player1", score);
				GD.Print($"[GameOver] Рекорд сохранён: {score}");
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[GameOver] Ошибка сохранения: {ex.Message}");
			}
		}
	}

	private void UpdateUI()
	{
		if (FinalScoreLabel != null)
			FinalScoreLabel.Text = $"Финальный счёт: {_finalScore}";

		if (HighScoreLabel != null)
			HighScoreLabel.Text = $"Рекорд: {_highScore}";
	}

	private void ConnectButtons()
	{
		if (RestartButton != null)
		{
			GD.Print("[GameOver] Подключаем RestartButton...");
			RestartButton.Pressed += OnRestartButtonPressed;
		}
		else
		{
			GD.PrintErr("[GameOver] RestartButton = null!");
		}

		if (MenuButton != null)
		{
			GD.Print("[GameOver] Подключаем MenuButton...");
			MenuButton.Pressed += OnMenuButtonPressed;
		}
		else
		{
			GD.PrintErr("[GameOver] MenuButton = null!");
		}

		if (QuitButton != null)
		{
			GD.Print("[GameOver] Подключаем QuitButton...");
			QuitButton.Pressed += OnQuitButtonPressed;
		}
		else
		{
			GD.PrintErr("[GameOver] QuitButton = null!");
		}
	}

	public void OnRestartButtonPressed()
	{
		GD.Print("[GameOver] Кнопка 'Играть снова' нажата!");
		GetTree().ChangeSceneToFile("res://Scenes/Game.tscn");
	}

	public void OnMenuButtonPressed()
	{
		GD.Print("[GameOver] Кнопка 'Главное меню' нажата!");
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}

	public void OnQuitButtonPressed()
	{
		GD.Print("[GameOver] Кнопка 'Выйти' нажата!");
		GetTree().Quit();
	}
}
