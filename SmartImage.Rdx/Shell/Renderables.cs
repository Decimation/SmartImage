// Author: Deci | Project: SmartImage.Rdx | Name: Renderables.cs
// Date: 2025/12/27 @ 22:12:49

#region

global using SizeIS = SixLabors.ImageSharp.Size;
using RU = Kantan.Console.RenderableUtility;
using EU = Kantan.Console.ElementUtility;

#endregion

using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Resources;
using System.Runtime.InteropServices;
using Flurl;
using Novus.Runtime;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Alloc;
using SmartImage.Lib.Model;
#nullable disable
using System.Reflection;
using Kantan.Text;
using Novus.OS;
using Novus.Utilities;
using SmartImage;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Utilities.Integration;
using Spectre.Console;
using Spectre.Console.Rendering;
using Kantan.Console;


namespace SmartImage.Rdx.Shell;

internal static partial class Renderables
{

	extension(SearchResult result)
	{

		public IEnumerable<IRenderable> GetMainRows()
		{
			Style style = result.Engine.Option.GetColor();

			return [new Text($"{result.Engine.Name}", style), new Text($"{result.Results.Count}")];
		}

		public IEnumerable<IList<IRenderable>> GetFullRows()
			=> result.Results.Select(static res => res.GetMainRows(true));

	}

	extension(IDimensions dim)
	{

		public IRenderable GetDimensions()
		{
			// ReSharper disable PossibleInvalidOperationException
			return dim.HasDimensions ? new Text($"{dim.Width}{Strings.Constants.MUL_SIGN}{dim.Height}") : ElementUtility.Txt_NA;
		}

	}

	// todo: select IRenderable objects with an additional layer after selecting object properties

	extension(IResultItem sri)
	{

		/*public IRenderable GetDimensions()
			=> ((IImage) sri).GetDimensions();*/

		public IRenderable GetSimilarity()
			=> RU.AsRenderable(sri.Similarity);

		public IRenderable GetUrl()
			=> Url.IsValid(sri.Url) ? MarkupLink(sri.Url) : ElementUtility.Txt_NA;

		public Grid GetItemInfoGrid()
		{
			// TODO

			var gr = new Grid();
			gr.AddColumns(2);

			var elems = sri.GetMainRows(false).ToList();

			elems.AddRange(sri.GetElementProperties(s_itemInfoGridNames));

			if (elems.Count % 2 != 0) {
				elems.Add(ElementUtility.Txt_NA);
			}

			for (int i = 0; i < elems.Count - 1; i += 2) {
				gr.AddRow(elems[i], elems[i + 1]);
			}

			return gr;
		}

		public IList<IRenderable> GetMainRows(bool full)
		{
			IRenderable name;

			if (full) {
				var i     = sri.Root.Results.IndexOf(sri);
				var style = sri.Root.Engine.Option.GetColor();

				name = new Text($"#{i}" + (sri.IsRaw ? " (Raw)" : null), style);
			}
			else {
				name = new Text($"{sri.Root.Engine.Name}");
			}


			var sim    = sri.GetSimilarity();
			var artist = RU.AsRenderable(sri.Artist);
			var wh     = sri.GetDimensions();
			var url    = sri.GetUrl();

			return [name, url, sim, artist, wh];
		}


		public IList<IRenderable> GetItemRow(int idx, int subIdx)
		{
			var result = sri.Root;
			var style  = new Style(foreground: result.Engine.Option.GetColor());
			var name   = new Markup($"[link={Markup.Escape(sri.Url)}]#{idx}.{subIdx}[/]", style);

			var rows = sri.GetMainRows(false);
			rows[0] = name;
			return rows;
		}

		public Grid CreateExtendedGrid()
		{
			// TODO: WIP
			var grid = new Grid();
			grid.AddColumns(2);

			var properties = sri.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
			                    .Where(static p => !p.IsCalculated())
			                    .Where(static x => Array.IndexOf(s_extGridNameBlacklist, x.Name) == -1);

			foreach (var property in properties) {
				var propVal = property.GetValue(sri);

				if (propVal == null || (propVal is string s && String.IsNullOrWhiteSpace(s))) {
					continue;
				}

				var render = RU.AsRenderable(propVal);
				grid.AddRow(new Text($"{property.Name}", ElementStyles.Sty_ResultHeader), render);
			}

			grid.AddRow(new Text("Resolution", ElementStyles.Sty_ResultHeader), sri.GetDimensions());

			return grid;
		}

		public IEnumerable<IRenderable> GetElementProperties(IEnumerable<string> propNames)
		{
			const BindingFlags OPTIONS = BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public;

			foreach (string name in propNames) {
				var prop = sri.GetType().GetProperty(name, OPTIONS);

				var propVal = prop?.GetValue(sri);

				if (propVal == null || (propVal is string s && String.IsNullOrWhiteSpace(s))) {
					continue;
				}

				yield return RU.AsRenderable(propVal);
			}
		}

	}

	extension(FigletFont)
	{

		[MURV]
		public static FigletFont LoadFromResource(ResourceManager rsrc, string name, out MemoryStream fs)
		{
			var o = rsrc.GetObject(name);

			if (o == null) {
				throw new InvalidOperationException(nameof(name));
			}

			fs = new MemoryStream((byte[]) o);
			var ff = FigletFont.Load(fs);

			return ff;
		}

	}

#region Renderable ElementStyles

	private static readonly string[] s_extGridNameBlacklist =
	[
		nameof(SearchResultItem.ScannedItems),
		nameof(IResultItem.IsRaw),
		nameof(IImage.Width),
		nameof(IImage.Height),
		nameof(SearchResultItem.Metadata),
		nameof(IChildResultItem.Parent),
		nameof(IAllocImage.Source),
		nameof(IResultItem.Url),

	];

	private static readonly Type[] s_extGridTypes =
	[
		typeof(IResultItem),
		typeof(ISimilarity),
		typeof(IHashable),
		typeof(IResultMetadata),
		typeof(IAllocImage)
	];

	private static readonly string[] s_itemInfoGridNames =
	[
		nameof(IResultMetadata.Character),
		nameof(IResultMetadata.Source),
		nameof(IResultMetadata.Description),
		nameof(IResultMetadata.Site),
		nameof(IResultMetadata.Title)
	];

#endregion


	

	public static readonly TextPrompt<string> Prm_Selection = new(Markup.Escape("[#.#]"))
	{
		ShowChoices      = false,
		ShowDefaultValue = false,
		AllowEmpty       = false,
	};

	public static readonly TextPrompt<string> Prm_Command = new(Markup.Escape("[Command]"))
	{
		ShowChoices      = true,
		ShowDefaultValue = true,
		AllowEmpty       = false,
		Choices =
		{
			R2.Chc_Open, R2.Chc_Scan, R2.Chc_Preview, R2.Chc_Calc, R2.Chc_Download, R2.Chc_Expand, R2.Chc_Back, R2.Chc_Exit
		}
	};

	public static readonly SelectionPrompt<SearchResult> Prm_SearchResult = new()
	{
		Mode                  = SelectionMode.Independent,
		SearchEnabled         = true,
		PageSize              = 1,
		MoreChoicesText       = "...",
		Title                 = null,
		SearchPlaceholderText = null,
		WrapAround            = true,
		Converter = static sr =>
		{
			//
			return sr.Engine.Name;
		}
	};

}

/// <summary>
/// <see cref="Renderables.GetItemRow"/>
/// </summary>
internal enum ResultRowIndex
{

	ROW_NUM        = 0,
	ROW_URL        = 1,
	ROW_SIMILARITY = 2,
	ROW_ARTIST     = 3,
	ROW_WH         = 4

}