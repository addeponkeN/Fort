using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using RectpackSharp;

namespace Fort.TexturePacker;

public class TexturePacker
{
	private readonly string _sourceFolder;
	private readonly string _destinationFile;

	public Color BackgroundColor { get; set; } = Color.Transparent;
	public int Spacing { get; set; } = 0;
	public bool WriteDataFile { get; set; } = true;
	public bool WriteDataIndented { get; set; } = false;

	public TexturePacker(string sourceFolder, string destinationFile)
	{
		_sourceFolder = sourceFolder;
		_destinationFile = destinationFile;
	}

	public TexturePackResult Pack()
	{
		var pngFiles = Directory.GetFiles(_sourceFolder, "*.png");
		var images = new List<Bitmap>();
		var rectangles = new PackingRectangle[pngFiles.Length];
		var regions = new List<TextureRegion>();

		for (int i = 0; i < pngFiles.Length; i++)
		{
			var image = new Bitmap(pngFiles[i]);
			images.Add(image);
			rectangles[i] = new PackingRectangle
			{
				Id = i,
				Width = (uint)(image.Width + Spacing),
				Height = (uint)(image.Height + Spacing)
			};
		}

		RectanglePacker.Pack(rectangles, out PackingRectangle bounds);

		var width = (int)bounds.Width;
		var height = (int)bounds.Height;

		using var resultImage = new Bitmap(width, height);
		using var graphics = Graphics.FromImage(resultImage);
		graphics.Clear(BackgroundColor);

		foreach (var rect in rectangles)
		{
			var image = images[rect.Id];
			graphics.DrawImage(image, (int)rect.X, (int)rect.Y);

			var region = new TextureRegion
			{
				name = Path.GetFileNameWithoutExtension(pngFiles[rect.Id]),
				frame = new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height)
			};
			regions.Add(region);
		}

		resultImage.Save(_destinationFile, ImageFormat.Png);

		var atlas = new TextureAtlas
		{
			name = Path.GetFileNameWithoutExtension(_destinationFile),
			regions = regions
		};

		string dataPath = string.Empty;

		if (WriteDataFile)
		{
			dataPath = Path.ChangeExtension(_destinationFile, ".json");
			var json = JsonSerializer.Serialize(atlas, new JsonSerializerOptions { WriteIndented = WriteDataIndented });
			File.WriteAllText(dataPath, json);
		}

		return new TexturePackResult
		{
			TexturePath = _destinationFile,
			DataPath = dataPath,
			Atlas = atlas
		};
	}
}
