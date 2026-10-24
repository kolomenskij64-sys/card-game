using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	[Export] public PackedScene CardScene;
	[Export] public Label PlayerHpLabel;
	[Export] public Label PlayerShieldLabel;
	[Export] public Label EnemyHpLabel;
	[Export] public Label EnemyNameLabel;
	[Export] public Label EnemyAttackLabel;
	[Export] public HBoxContainer HandContainer;
	[Export] public GridContainer BoardGrid;
	[Export] public ProgressBar EnemyHpBar;
	[Export] public Label EnemyHpText;

	public int PlayerHP = 20;
	public int EnemyHP = 30;
	public int PlayerShield = 0;
	public int Score = 0;
	public const int MaxPlayerHP = 50;

	private Enemy _currentEnemy;
	private int _turnCount = 0;
	private int _currentEnemyIndex = 0;

	private List<CardDataModel> _deck = new List<CardDataModel>();
	private List<CardDataModel> _hand = new List<CardDataModel>();
	private Random _random = new Random();

	private List<Enemy> _enemies = new List<Enemy>
	{
		new Enemy { Name = "Гоблин-разведчик", MaxHP = 20, Damage = 3, Description = "Слабый, но хитрый", AttackName = "Тычок ножом", StrongAttackName = "ОТРАВЛЕННЫЙ УДАР", Reward = 50, IsBoss = false },
		new Enemy { Name = "Скелет-воин", MaxHP = 30, Damage = 5, Description = "Костяной страж", AttackName = "Удар мечом", StrongAttackName = "КРУШАЩИЙ УДАР", Reward = 80, IsBoss = false },
		new Enemy { Name = "Орк-берсерк", MaxHP = 45, Damage = 7, Description = "Яростный воин", AttackName = "Удар топором", StrongAttackName = "ЯРОСТНЫЙ РАЗРУБ", Reward = 120, IsBoss = false },
		new Enemy { Name = "Тёмный маг", MaxHP = 35, Damage = 10, Description = "Владеет запретной магией", AttackName = "Тёмная стрела", StrongAttackName = "ОГНЕННЫЙ ШАР", Reward = 150, IsBoss = false },
		new Enemy { Name = "Дракон-предводитель", MaxHP = 60, Damage = 9, Description = "Старый и мудрый дракон", AttackName = "Удар хвостом", StrongAttackName = "ОГНЕВЫДОХ", Reward = 200, IsBoss = false },
		new Enemy { Name = "👑 КОРОЛЬ ДЕМОНОВ", MaxHP = 100, Damage = 12, Description = "Владыка тьмы. Финальное испытание!", AttackName = "Когти тьмы", StrongAttackName = "АПОКАЛИПСИС", Reward = 500, IsBoss = true }
	};

	public override void _Ready()
	{
		Instance = this;
		if (CardScene == null) { GD.PrintErr("CardScene не задан!"); return; }
		if (HandContainer == null) { GD.PrintErr("HandContainer не задан!"); return; }
		if (BoardGrid == null) { GD.PrintErr("BoardGrid не задан!"); return; }

		InitializeGameAsync();
	}

	private async void InitializeGameAsync()
	{
		GD.Print("Загрузка карт из БД...");
		try
		{
			_deck = await DatabaseAPI.Instance.FetchCardsAsync();
			GD.Print($"Загружено {_deck.Count} типов карт.");
			if (_deck.Count == 0) { GD.PrintErr("Колода пуста!"); return; }

			_currentEnemyIndex = 0;
			LoadCurrentEnemy();
		}
		catch (Exception ex)
		{
			GD.PrintErr("Ошибка инициализации: " + ex.Message);
		}
	}

	private void LoadCurrentEnemy()
	{
		_currentEnemy = _enemies[_currentEnemyIndex];
		EnemyHP = _currentEnemy.MaxHP;
		_turnCount = 0;
		
		GD.Print($"==================================================");
		GD.Print($"=== ЭТАП {_currentEnemyIndex + 1}/{_enemies.Count}: {_currentEnemy.Name} ===");
		GD.Print($"HP: {EnemyHP}/{_currentEnemy.MaxHP}, Урон: {_currentEnemy.Damage}");
		GD.Print($"Описание: {_currentEnemy.Description}");
		GD.Print($"==================================================");

		ClearHand();
		ClearBoard();

		ShowEnemyIntent(false);
		DrawCards(3);
		UpdateUI();
	}

	private void ClearHand()
	{
		_hand.Clear();
		foreach (Node child in HandContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void ShowEnemyIntent(bool isStrongAttack)
	{
		if (EnemyNameLabel != null)
			EnemyNameLabel.Text = _currentEnemy?.Name ?? "???";
		
		if (EnemyAttackLabel != null && _currentEnemy != null)
		{
			string attackName = isStrongAttack ? _currentEnemy.StrongAttackName : _currentEnemy.AttackName;
			int damage = isStrongAttack ? _currentEnemy.Damage + 3 : _currentEnemy.Damage;
			EnemyAttackLabel.Text = $"⚔️ {damage} урона!\n{attackName}";
		}
	}

	public void DrawCards(int count)
	{
		if (HandContainer == null || _deck.Count == 0 || CardScene == null) return;

		var playableDeck = _deck.Where(c => c.Level < 3).ToList();
		if (playableDeck.Count == 0) return;

		for (int i = 0; i < count; i++)
		{
			try
			{
				int randomIndex = _random.Next(playableDeck.Count);
				var randomCardData = playableDeck[randomIndex];
				_hand.Add(randomCardData);

				var cardInstance = (Card)CardScene.Instantiate();
				cardInstance.Setup(randomCardData);
				HandContainer.AddChild(cardInstance);
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[DrawCards] Ошибка: {ex.Message}");
			}
		}
	}

	public void OnCardPlaced(CardDataModel placedCard)
	{
		_hand.Remove(placedCard);
		foreach (Node child in HandContainer.GetChildren())
		{
			if (child is Card card && card.Data == placedCard)
			{
				child.QueueFree();
				break;
			}
		}
	}

	// ДОБАВЛЕНО: Метод для поиска карты по элементу и уровню
	public CardDataModel GetCardByElementAndLevel(string element, int level)
	{
		return _deck.FirstOrDefault(d => d.Element == element && d.Level == level);
	}

	private void _on_end_turn_button_pressed()
	{
		GD.Print("=== ХОД ЗАВЕРШЁН ===");
		_turnCount++;

		ApplyBoardEffects();
		ClearBoard();

		if (EnemyHP <= 0)
		{
			GD.Print("ПОБЕДА НАД ВРАГОМ!");
			OnVictory();
			return;
		}

		bool isStrongAttack = _turnCount % 3 == 0;
		int enemyDamage = isStrongAttack ? _currentEnemy.Damage + 3 : _currentEnemy.Damage;
		
		GD.Print($"[{_currentEnemy.Name}] Атакует: {(isStrongAttack ? _currentEnemy.StrongAttackName : _currentEnemy.AttackName)} ({enemyDamage} урона!)");
		
		if (PlayerShield > 0)
		{
			if (PlayerShield >= enemyDamage)
			{
				PlayerShield -= enemyDamage;
				GD.Print($"🛡️ Щит поглотил весь урон. Осталось щита: {PlayerShield}");
			}
			else
			{
				int rest = enemyDamage - PlayerShield;
				PlayerShield = 0;
				PlayerHP -= rest;
				GD.Print($"🛡️ Щит поглотил часть. Остаток {rest} пошёл в HP. HP: {PlayerHP}");
			}
		}
		else
		{
			PlayerHP -= enemyDamage;
			GD.Print($"Урон по HP: {enemyDamage}. Осталось HP: {PlayerHP}");
		}

		DrawCards(1);
		UpdateUI();
		
		bool nextIsStrong = (_turnCount + 1) % 3 == 0;
		ShowEnemyIntent(nextIsStrong);

		if (PlayerHP <= 0)
		{
			GD.Print("Game Over!");
			OnGameOver();
		}
	}

	private void ClearBoard()
{
	if (BoardGrid == null) return;
	
	var zones = BoardGrid.GetChildren().Cast<DropZone>().ToList();
	int removedCount = 0;
	int keptCount = 0;
	
	foreach (var zone in zones)
	{
		// Проверяем, есть ли в ячейке живая карта
		if (zone.OccupyingCard != null && IsInstanceValid(zone.OccupyingCard))
		{
			int level = zone.OccupyingCard.Data.Level;
			
			if (level >= 3)
			{
				// Карты 3 уровня исчезают
				GD.Print($"[ClearBoard] Карта {zone.OccupyingCard.Data.Element} Lvl {level} исчезает");
				zone.ClearZone(); // Используем новый метод
				removedCount++;
			}
			else
			{
				// Карты 1-2 уровней остаются
				GD.Print($"[ClearBoard] Карта {zone.OccupyingCard.Data.Element} Lvl {level} остаётся");
				keptCount++;
			}
		}
		else if (zone.OccupyingCard != null && !IsInstanceValid(zone.OccupyingCard))
		{
			// Очищаем мёртвую ссылку
			GD.Print($"[ClearBoard] {zone.Name}: очищаем мёртвую ссылку");
			zone.OccupyingCard = null;
		}
	}
	
	GD.Print($"[ClearBoard] Удалено: {removedCount}, Осталось: {keptCount}");
}
	private void ApplyBoardEffects()
	{
		if (BoardGrid == null) return;
		var zones = BoardGrid.GetChildren().Cast<DropZone>().ToList();
		int totalDamage = 0, totalHeal = 0, totalShield = 0;

		foreach (var zone in zones)
		{
			if (zone.OccupyingCard == null || zone.OccupyingCard.Data == null) continue;
			var d = zone.OccupyingCard.Data;

			switch (d.Element)
			{
				case "Fire": totalDamage += d.Damage; break;
				case "Water": totalHeal += d.Damage; break;
				case "Earth": totalShield += d.Damage; break;
			}
		}

		if (totalDamage > 0) { EnemyHP -= totalDamage; if (EnemyHP < 0) EnemyHP = 0; }
		if (totalHeal > 0) { PlayerHP += totalHeal; if (PlayerHP > MaxPlayerHP) PlayerHP = MaxPlayerHP; }
		if (totalShield > 0) { PlayerShield += totalShield; }
	}

	private void OnVictory()
	{
		Score += _currentEnemy.Reward;
		GD.Print($"Победа! Получено {_currentEnemy.Reward} очков. Всего: {Score}");

		if (_currentEnemy.IsBoss)
		{
			GD.Print("🏆 ИГРА ПРОЙДЕНА! КОРОЛЬ ДЕМОНОВ ПОВЕРЖЕН! 🏆");
			GameOverData.FinalScore = Score;
			GameOverData.IsVictory = true;
			GetTree().ChangeSceneToFile("res://Scenes/GameOver.tscn");
		}
		else
		{
			GD.Print("Переход к следующему противнику...");
			_currentEnemyIndex++;
			PlayerHP = Math.Min(PlayerHP + 5, MaxPlayerHP);
			GD.Print($"Отдых между боями: +5 HP. Текущее HP: {PlayerHP}");
			LoadCurrentEnemy();
		}
	}

	private void OnGameOver()
	{
		GD.Print($"Game Over! Финальный счёт: {Score}");
		GameOverData.FinalScore = Score;
		GameOverData.IsVictory = false;
		GetTree().ChangeSceneToFile("res://Scenes/GameOver.tscn");
	}

	public void UpdateUI()
	{
		if (PlayerHpLabel != null) PlayerHpLabel.Text = $"Здоровье: {PlayerHP}";
		if (PlayerShieldLabel != null) PlayerShieldLabel.Text = PlayerShield > 0 ? $"🛡️ {PlayerShield}" : "";
		
		if (EnemyHpLabel != null && _currentEnemy != null)
			EnemyHpLabel.Text = $"Здоровье врага: {EnemyHP}/{_currentEnemy.MaxHP}";

		if (EnemyHpBar != null && _currentEnemy != null)
			EnemyHpBar.MaxValue = _currentEnemy.MaxHP;
			
		if (EnemyHpBar != null) EnemyHpBar.Value = EnemyHP;
		if (EnemyHpText != null) EnemyHpText.Text = $"HP: {EnemyHP}";
	}

	// ДОБАВЛЕНО: Публичный метод для обновления UI (алиас на UpdateUI)
	public void UpdateUIPublic()
	{
		UpdateUI();
	}
}

public class Enemy
{
	public string Name { get; set; }
	public int MaxHP { get; set; }
	public int Damage { get; set; }
	public string Description { get; set; }
	public string AttackName { get; set; }
	public string StrongAttackName { get; set; }
	public int Reward { get; set; }
	public bool IsBoss { get; set; }
}

public static class GameOverData
{
	public static int FinalScore { get; set; } = 0;
	public static bool IsVictory { get; set; } = false;
}
