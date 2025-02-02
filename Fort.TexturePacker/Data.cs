using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fort.TexturePacker;

public class TexturePackResult
{
	public string TexturePath { get; set; }
	public string DataPath { get; set; }
	public TextureAtlas Atlas { get; set; }
}

public class TextureAtlas
{
	public string name { get; set; }
	public List<TextureRegion> regions { get; set; }
}

public class TextureRegion
{
	public string name { get; set; }
	[JsonConverter(typeof(JsonRectangleConverter))]
	public Rectangle frame { get; set; }
}

public class JsonRectangleConverter : JsonConverter<Rectangle>
{
	public override Rectangle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.EndObject)
			return Rectangle.Empty;

		var split = reader.GetString().Split(',');

		var rec = new Rectangle();
		rec.X = int.Parse(split[0]);
		rec.Y = int.Parse(split[1]);
		rec.Width = int.Parse(split[2]);
		rec.Height = int.Parse(split[3]);

		return rec;
	}

	public override void Write(Utf8JsonWriter writer, Rectangle value, JsonSerializerOptions options)
	{
		writer.WriteStringValue($"{value.X},{value.Y},{value.Width},{value.Height}");
	}
}