using Godot;
using System;

public partial class DropZone : PanelContainer
{
	public Card OccupyingCard { get; set; }

	public override void _Ready()
	{
		GD.Print($"[DropZone._Ready] {Name} готов");
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		// Проверяем, что data — это карта
		if (data.Obj is not Card card)
		{
			return false;
		}
		
		// Проверяем, что карта ещё в дереве сцены
		if (!IsInstanceValid(card))
		{
			GD.PrintErr($"[DropZone._CanDropData] {Name}: карта невалидна!");
			return false;
		}
		
		// Проверяем, что ячейка пуста ИЛИ карта ещё не удалена
		bool isEmpty = OccupyingCard == null || !IsInstanceValid(OccupyingCard);
		
		if (!isEmpty)
		{
			GD.Print($"[DropZone._CanDropData] {Name}: ячейка занята!");
		}
		
		return isEmpty;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		GD.Print($"[DropZone._DropData] {Name} получил данные");
		
		if (data.Obj is not Card card)
		{
			GD.PrintErr($"[DropZone._DropData] {Name}: это не карта!");
			return;
		}
		
		if (!IsInstanceValid(card))
		{
			GD.PrintErr($"[DropZone._DropData] {Name}: карта невалидна!");
			return;
		}
		
		if (card.Data == null)
		{
			GD.PrintErr($"[DropZone._DropData] {Name}: у карты нет Data!");
			return;
		}
		
		// Проверяем, занята ли ячейка
		if (OccupyingCard != null && IsInstanceValid(OccupyingCard))
		{
			GD.PrintErr($"[DropZone._DropData] {Name} уже занята картой {OccupyingCard.Data.Element}!");
			return;
		}
		
		// Очищаем старую ссылку если объект мёртв
		if (OccupyingCard != null && !IsInstanceValid(OccupyingCard))
		{
			GD.Print($"[DropZone._DropData] {Name}: очищаем мёртвую ссылку");
			OccupyingCard = null;
		}
		
		// Удаляем карту из родителя (руки)
		if (card.GetParent() != null)
		{
			GD.Print($"[DropZone._DropData] Удаляем карту из {card.GetParent().Name}");
			card.GetParent().RemoveChild(card);
		}
		else
		{
			GD.PrintErr($"[DropZone._DropData] У карты нет родителя!");
			return;
		}
		
		// Добавляем карту в ячейку
		AddChild(card);
		card.Position = Vector2.Zero;
		OccupyingCard = card;
		
		GD.Print($"[DropZone._DropData] Карта {card.Data.Element} размещена в {Name}");
		
		// Уведомляем GameManager
		if (GameManager.Instance != null)
		{
			GameManager.Instance.OnCardPlaced(card.Data);
		}
		else
		{
			GD.PrintErr("[DropZone._DropData] GameManager.Instance = null!");
		}
	}
	
	/// <summary>
	/// Очищает ячейку — вызывается из GameManager при необходимости
	/// </summary>
	public void ClearZone()
	{
		if (OccupyingCard != null && IsInstanceValid(OccupyingCard))
		{
			GD.Print($"[DropZone.Clear] {Name}: удаляем карту {OccupyingCard.Data.Element}");
			OccupyingCard.QueueFree();
		}
		OccupyingCard = null;
		GD.Print($"[DropZone.Clear] {Name}: очищена");
	}
}
