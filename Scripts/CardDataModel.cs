using System.Text.Json.Serialization;

public class CardDataModel
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("element")]
	public string Element { get; set; } = "";

	[JsonPropertyName("level")]
	public int Level { get; set; }

	[JsonPropertyName("sprite_path")]
	public string SpritePath { get; set; } = "";

	[JsonPropertyName("damage")]
	public int Damage { get; set; }

	[JsonPropertyName("created_at")]
	public string CreatedAt { get; set; } = "";
}
