using Godot;
using System;

public partial class Card : PanelContainer
{
	public CardDataModel Data { get; set; }
	
	private TextureRect _sprite;
	private Label _damageLabel;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<TextureRect>("VBoxContainer/Sprite2D");
		_damageLabel = GetNodeOrNull<Label>("VBoxContainer/DamageLabel");
		
		if (_sprite == null)
			GD.PrintErr("[Card._Ready] Sprite2D НЕ НАЙДЕН!");
		else
			GD.Print("[Card._Ready] Sprite2D найден");
		
		if (_damageLabel == null)
			GD.PrintErr("[Card._Ready] DamageLabel НЕ НАЙДЕН!");
		else
			GD.Print("[Card._Ready] DamageLabel найден");
		
		CustomMinimumSize = new Vector2(80, 100);
		
		if (Data != null)
		{
			ApplySetup(Data);
		}
	}

	public void Setup(CardDataModel data)
	{
		Data = data;
		if (IsInsideTree())
		{
			ApplySetup(data);
		}
	}

	private void ApplySetup(CardDataModel data)
	{
		if (_sprite == null || _damageLabel == null)
		{
			GD.PrintErr("[Card.ApplySetup] Ноды не найдены!");
			return;
		}
		
		if (!string.IsNullOrEmpty(data.SpritePath) && ResourceLoader.Exists(data.SpritePath))
		{
			_sprite.Texture = GD.Load<Texture2D>(data.SpritePath);
		}
		else
		{
			var image = Image.CreateEmpty(80, 60, false, Image.Format.Rgba8);
			Color color = data.Element switch
			{
				"Fire" => new Color(1f, 0.3f, 0.2f),
				"Water" => new Color(0.2f, 0.5f, 1f),
				"Earth" => new Color(0.6f, 0.4f, 0.2f),
				_ => new Color(0.8f, 0.8f, 0.8f)
			};
			image.Fill(color);
			_sprite.Texture = ImageTexture.CreateFromImage(image);
		}
		
		string effectText = data.Element switch
		{
			"Fire" => $"Урон: {data.Damage}",
			"Water" => $"Лечение: {data.Damage}",
			"Earth" => $"Щит: {data.Damage}",
			_ => $"Урон: {data.Damage}"
		};
		
		_damageLabel.Text = effectText;
		_damageLabel.AddThemeFontSizeOverride("font_size", 14);
	}

	// ===== DRAG & DROP =====
	
	public override Variant _GetDragData(Vector2 atPosition)
	{
		GD.Print($"[Card._GetDragData] Начало перетаскивания: {Data?.Element} Lvl {Data?.Level}");
		
		if (_sprite?.Texture == null) return default;
		
		var preview = new TextureRect();
		preview.Texture = _sprite.Texture;
		preview.CustomMinimumSize = new Vector2(80, 100);
		SetDragPreview(preview);
		
		return this;
	}

	/// <summary>
	/// Проверяем, можно ли бросить карту НА ЭТУ карту (для слияния)
	/// </summary>
	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (data.Obj is not Card draggedCard) return false;
		if (draggedCard.Data == null || this.Data == null) return false;
		
		// Разрешаем drop только если можно слить
		bool canMerge = this.Data.Element == draggedCard.Data.Element &&
					   this.Data.Level == draggedCard.Data.Level &&
					   this.Data.Level < 3;
		
		GD.Print($"[Card._CanDropData] {this.Data.Element} Lvl {this.Data.Level} + {draggedCard.Data.Element} Lvl {draggedCard.Data.Level} -> canMerge={canMerge}");
		return canMerge;
	}

	/// <summary>
	/// Выполняем слияние когда карту бросают на эту карту
	/// </summary>
	public override void _DropData(Vector2 atPosition, Variant data)
	{
		GD.Print($"[Card._DropData] Получен drop на карту {Data?.Element} Lvl {Data?.Level}");
		
		if (data.Obj is not Card draggedCard) return;
		if (draggedCard.Data == null || this.Data == null) return;
		
		var existingData = this.Data;
		var draggedData = draggedCard.Data;
		
		// Проверяем условия слияния ещё раз
		if (existingData.Element != draggedData.Element || 
			existingData.Level != draggedData.Level || 
			existingData.Level >= 3)
		{
			GD.PrintErr("[Card._DropData] Условия слияния не выполнены!");
			return;
		}
		
		int newLevel = existingData.Level + 1;
		GD.Print($"[Card._DropData] ✅ СЛИЯНИЕ: {existingData.Element} Lvl {existingData.Level} + Lvl {draggedData.Level} -> Lvl {newLevel}");
		
		// Находим DropZone родитель этой карты
		var parentDropZone = GetParent() as DropZone;
		if (parentDropZone == null)
		{
			GD.PrintErr("[Card._DropData] Родитель не DropZone!");
			return;
		}
		
		// Удаляем обе карты
		if (draggedCard.GetParent() != null)
		{
			draggedCard.GetParent().RemoveChild(draggedCard);
		}
		draggedCard.QueueFree();
		
		if (this.GetParent() != null)
		{
			this.GetParent().RemoveChild(this);
		}
		this.QueueFree();
		
		parentDropZone.OccupyingCard = null;
		
		// Создаём новую карту
		if (GameManager.Instance != null)
		{
			var mergedData = GameManager.Instance.GetCardByElementAndLevel(existingData.Element, newLevel);
			
			if (mergedData != null)
			{
				GD.Print($"[Card._DropData] Создаём карту {mergedData.Element} Lvl {mergedData.Level}");
				
				var newCard = (Card)GameManager.Instance.CardScene.Instantiate();
				newCard.Setup(mergedData);
				parentDropZone.AddChild(newCard);
				newCard.Position = Vector2.Zero;
				parentDropZone.OccupyingCard = newCard;
				
				// Анимация
				newCard.Scale = Vector2.Zero;
				var tween = CreateTween();
				tween.TweenProperty(newCard, "scale", Vector2.One, 0.3f)
					 .SetTrans(Tween.TransitionType.Elastic)
					 .SetEase(Tween.EaseType.Out);
				
				GameManager.Instance.Score += mergedData.Level * 10;
				GameManager.Instance.UpdateUIPublic();
				
				GD.Print($"[Card._DropData] Слияние завершено! +{mergedData.Level * 10} очков");
			}
			else
			{
				GD.PrintErr($"[Card._DropData] Карта {existingData.Element} Lvl {newLevel} НЕ НАЙДЕНА в колоде!");
			}
		}
	}
}
