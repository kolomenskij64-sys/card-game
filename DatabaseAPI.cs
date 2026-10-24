using Godot;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public partial class DatabaseAPI : Node
{
	public static DatabaseAPI Instance { get; private set; }

	// ⚠️ ЗАМЕНИ НА СВОИ ДАННЫЕ ИЗ SUPABASE
	private string projectUrl = "https://zrgkdtcksuvohbxrbvrh.supabase.co";
	private string apiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InpyZ2tkdGNrc3V2b2hieHJidnJoIiwicm9sZSI6ImFub24iLCJpYXQiOjE3OTE3OTcyNTcsImV4cCI6MjEwNzM3MzI1N30.pbPtiFND2IL6vfV-2J-F2MbDp66ndPJxymdgvUhCQIA";

	private System.Net.Http.HttpClient _httpClient;

	public override void _Ready()
	{
		Instance = this;
		_httpClient = new System.Net.Http.HttpClient();
		_httpClient.DefaultRequestHeaders.Add("apikey", apiKey);
		_httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
		// Отключаем кэширование через заголовки
		_httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache, no-store, must-revalidate");
		_httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
		GD.Print("DatabaseAPI (SUPABASE) инициализирован");
	}

	/// <summary>
	/// Загружает все карты из таблицы card_assets
	/// </summary>
	public async Task<List<CardDataModel>> FetchCardsAsync()
	{
		try
		{
			string url = $"{projectUrl}/rest/v1/card_assets?select=*";
			HttpResponseMessage response = await _httpClient.GetAsync(url);
			response.EnsureSuccessStatusCode();

			string json = await response.Content.ReadAsStringAsync();
			GD.Print($"[DB] Получено JSON: {json}");

			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};

			var cards = JsonSerializer.Deserialize<List<CardDataModel>>(json, options);
			return cards ?? new List<CardDataModel>();
		}
		catch (Exception ex)
		{
			GD.PrintErr("[DB] Ошибка загрузки карт: " + ex.Message);
			return new List<CardDataModel>();
		}
	}

	/// <summary>
	/// Сохраняет рекорд игрока в БД (UPSERT — вставка или обновление)
	/// </summary>
	public async Task UpdatePlayerScoreAsync(string username, int score)
	{
		try
		{
			GD.Print($"[DB] Сохранение рекорда: {username} = {score}");

			string url = $"{projectUrl}/rest/v1/player_progress";

			// UPSERT: если запись есть — обновит, если нет — создаст
			string body = JsonSerializer.Serialize(new { username = username, high_score = score });

			var content = new StringContent(body, Encoding.UTF8, "application/json");
			content.Headers.Remove("Prefer");
			content.Headers.Add("Prefer", "return=representation");

			// POST с upsert=on_conflict
			HttpResponseMessage response = await _httpClient.PostAsync(
				$"{url}?username=eq.{username}&upsert=on_conflict",
				content
			);

			string responseJson = await response.Content.ReadAsStringAsync();
			GD.Print($"[DB] Ответ сервера: {(int)response.StatusCode} {responseJson}");

			response.EnsureSuccessStatusCode();
			GD.Print($"[DB] Рекорд сохранён: {username} = {score}");
		}
		catch (Exception ex)
		{
			GD.PrintErr("[DB] Ошибка сохранения рекорда: " + ex.Message);
			throw;
		}
	}

	/// <summary>
	/// Загружает рекорд игрока из БД (БЕЗ параметра _t!)
	/// </summary>
	public async Task<int> GetHighScoreAsync(string username)
	{
		try
		{
			// ВАЖНО: без &_t={timestamp} — Supabase не понимает этот параметр
			string url = $"{projectUrl}/rest/v1/player_progress?username=eq.{username}&select=high_score";

			HttpResponseMessage response = await _httpClient.GetAsync(url);
			response.EnsureSuccessStatusCode();
			string json = await response.Content.ReadAsStringAsync();

			GD.Print($"[DB] Загрузка рекорда: {json}");

			if (string.IsNullOrEmpty(json) || json == "[]")
				return 0;

			var result = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);
			if (result != null && result.Count > 0 && result[0].ContainsKey("high_score"))
			{
				return Convert.ToInt32(result[0]["high_score"]);
			}

			return 0;
		}
		catch (Exception ex)
		{
			GD.PrintErr("[DB] Ошибка загрузки рекорда: " + ex.Message);
			return 0;
		}
	}
}
