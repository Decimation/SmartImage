using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace SmartImage.UI.Controls;

public class GridViewSort
{

	#region Public attached properties

	public static ICommand GetCommand(DependencyObject obj)
	{
		return (ICommand) obj.GetValue(CommandProperty);
	}

	public static void SetCommand(DependencyObject obj, ICommand value)
	{
		obj.SetValue(CommandProperty, value);
	}

	// Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
	public static readonly DependencyProperty CommandProperty =
		DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(GridViewSort),
		                                    new UIPropertyMetadata(null, OnChanged));

	private static void OnChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
	{

		if (o is not ItemsControl listView) return;

		if (GetAutoSort(listView)) return; // Don't change click handler if AutoSort enabled

		if (e.OldValue != null && e.NewValue == null) {
			listView.RemoveHandler(ButtonBase.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
		}

		if (e.OldValue == null && e.NewValue != null) {
			listView.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
		}
	}

	public static bool GetAutoSort(DependencyObject obj)
	{
		return (bool) obj.GetValue(AutoSortProperty);
	}

	public static void SetAutoSort(DependencyObject obj, bool value)
	{
		obj.SetValue(AutoSortProperty, value);
	}

	// Using a DependencyProperty as the backing store for AutoSort.  This enables animation, styling, binding, etc...
	public static readonly DependencyProperty AutoSortProperty =
		DependencyProperty.RegisterAttached("AutoSort", typeof(bool), typeof(GridViewSort),
		                                    new UIPropertyMetadata(false, OnChanged2));

	private static void OnChanged2(DependencyObject o, DependencyPropertyChangedEventArgs e)
	{

		if (o is ListView listView) {
			if (GetCommand(listView) == null) // Don't change click handler if a command is set
			{
				bool oldValue = (bool) e.OldValue;
				bool newValue = (bool) e.NewValue;

				if (oldValue && !newValue) {
					listView.RemoveHandler(ButtonBase.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
				}

				if (!oldValue && newValue) {
					listView.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
				}
			}
		}
	}

	public static string GetPropertyName(DependencyObject obj)
	{
		return (string) obj.GetValue(PropertyNameProperty);
	}

	public static void SetPropertyName(DependencyObject obj, string value)
	{
		obj.SetValue(PropertyNameProperty, value);
	}

	// Using a DependencyProperty as the backing store for PropertyName.  This enables animation, styling, binding, etc...
	public static readonly DependencyProperty PropertyNameProperty =
		DependencyProperty.RegisterAttached(
			"PropertyName",
			typeof(string),
			typeof(GridViewSort),
			new UIPropertyMetadata(null)
		);

	public static bool GetShowSortGlyph(DependencyObject obj)
	{
		return (bool) obj.GetValue(ShowSortGlyphProperty);
	}

	public static void SetShowSortGlyph(DependencyObject obj, bool value)
	{
		obj.SetValue(ShowSortGlyphProperty, value);
	}

	// Using a DependencyProperty as the backing store for ShowSortGlyph.  This enables animation, styling, binding, etc...
	public static readonly DependencyProperty ShowSortGlyphProperty =
		DependencyProperty.RegisterAttached("ShowSortGlyph", typeof(bool), typeof(GridViewSort),
		                                    new UIPropertyMetadata(true));

	public static ImageSource GetSortGlyphAscending(DependencyObject obj)
	{
		return (ImageSource) obj.GetValue(SortGlyphAscendingProperty);
	}

	public static void SetSortGlyphAscending(DependencyObject obj, ImageSource value)
	{
		obj.SetValue(SortGlyphAscendingProperty, value);
	}

	// Using a DependencyProperty as the backing store for SortGlyphAscending.  This enables animation, styling, binding, etc...
	public static readonly DependencyProperty SortGlyphAscendingProperty =
		DependencyProperty.RegisterAttached("SortGlyphAscending", typeof(ImageSource), typeof(GridViewSort),
		                                    new UIPropertyMetadata(null));

	public static ImageSource GetSortGlyphDescending(DependencyObject obj)
	{
		return (ImageSource) obj.GetValue(SortGlyphDescendingProperty);
	}

	public static void SetSortGlyphDescending(DependencyObject obj, ImageSource value)
	{
		obj.SetValue(SortGlyphDescendingProperty, value);
	}

	// Using a DependencyProperty as the backing store for SortGlyphDescending.  This enables animation, styling, binding, etc...
	public static readonly DependencyProperty SortGlyphDescendingProperty =
		DependencyProperty.RegisterAttached("SortGlyphDescending", typeof(ImageSource), typeof(GridViewSort),
		                                    new UIPropertyMetadata(null));

	#endregion

	#region Private attached properties

	private static GridViewColumnHeader GetSortedColumnHeader(DependencyObject obj)
	{
		return (GridViewColumnHeader) obj.GetValue(SortedColumnHeaderProperty);
	}

	private static void SetSortedColumnHeader(DependencyObject obj, GridViewColumnHeader value)
	{
		obj.SetValue(SortedColumnHeaderProperty, value);
	}

	// Using a DependencyProperty as the backing store for SortedColumn.  This enables animation, styling, binding, etc...
	private static readonly DependencyProperty SortedColumnHeaderProperty =
		DependencyProperty.RegisterAttached("SortedColumnHeader", typeof(GridViewColumnHeader),
		                                    typeof(GridViewSort), new UIPropertyMetadata(null));

	#endregion

	#region Column header click event handler

	private static void ColumnHeader_Click(object sender, RoutedEventArgs e)
	{

		if (e.OriginalSource is GridViewColumnHeader headerClicked && headerClicked.Column != null) {
			string propertyName = GetPropertyName(headerClicked.Column);

			if (!String.IsNullOrEmpty(propertyName)) {
				var listView = GetAncestor<ListView>(headerClicked);

				if (listView != null) {
					ICommand command = GetCommand(listView);

					if (command != null) {
						if (command.CanExecute(propertyName)) {
							command.Execute(propertyName);
						}
					}
					else if (GetAutoSort(listView)) {
						ApplySort(listView.Items, propertyName, listView, headerClicked);
					}
				}
			}
		}
	}

	#endregion

	#region Helper methods

	public static T GetAncestor<T>(DependencyObject reference) where T : DependencyObject
	{
		DependencyObject parent = VisualTreeHelper.GetParent(reference);

		while (parent is not T) {
			parent = VisualTreeHelper.GetParent(parent);
		}

		return (T) parent;
	}

	public static void ApplySort(ICollectionView view, string propertyName, ListView listView,
	                             GridViewColumnHeader sortedColumnHeader)
	{
		var direction = ListSortDirection.Ascending;

		if (view.SortDescriptions.Count > 0) {
			SortDescription currentSort = view.SortDescriptions[0];

			if (currentSort.PropertyName == propertyName) {
				if (currentSort.Direction == ListSortDirection.Ascending)
					direction = ListSortDirection.Descending;
				else
					direction = ListSortDirection.Ascending;
			}

			view.SortDescriptions.Clear();

			GridViewColumnHeader currentSortedColumnHeader = GetSortedColumnHeader(listView);

			if (currentSortedColumnHeader != null) {
				RemoveSortGlyph(currentSortedColumnHeader);
			}
		}

		if (!String.IsNullOrEmpty(propertyName)) {
			view.SortDescriptions.Add(new SortDescription(propertyName, direction));

			if (GetShowSortGlyph(listView))
				AddSortGlyph(
					sortedColumnHeader,
					direction,
					direction == ListSortDirection.Ascending
						? GetSortGlyphAscending(listView)
						: GetSortGlyphDescending(listView));
			SetSortedColumnHeader(listView, sortedColumnHeader);
		}
	}

	private static void AddSortGlyph(GridViewColumnHeader columnHeader, ListSortDirection direction,
	                                 ImageSource sortGlyph)
	{
		var adornerLayer = AdornerLayer.GetAdornerLayer(columnHeader);

		adornerLayer.Add(
			new SortGlyphAdorner(
				columnHeader,
				direction,
				sortGlyph
			));
	}

	private static void RemoveSortGlyph(GridViewColumnHeader columnHeader)
	{
		var       adornerLayer = AdornerLayer.GetAdornerLayer(columnHeader);
		Adorner[] adorners     = adornerLayer.GetAdorners(columnHeader);

		if (adorners != null) {
			foreach (Adorner adorner in adorners) {
				if (adorner is SortGlyphAdorner)
					adornerLayer.Remove(adorner);
			}
		}
	}

	#endregion

	#region SortGlyphAdorner nested class

	private class SortGlyphAdorner : Adorner
	{

		private readonly GridViewColumnHeader m_columnHeader;
		private readonly ListSortDirection    m_direction;
		private readonly ImageSource          m_sortGlyph;

		public SortGlyphAdorner(GridViewColumnHeader columnHeader, ListSortDirection direction,
		                        ImageSource sortGlyph)
			: base(columnHeader)
		{
			m_columnHeader = columnHeader;
			m_direction    = direction;
			m_sortGlyph    = sortGlyph;
		}

		private Geometry GetDefaultGlyph()
		{
			double x1 = m_columnHeader.ActualWidth - 13;
			double x2 = x1 + 10;
			double x3 = x1 + 5;
			double y1 = m_columnHeader.ActualHeight / 2 - 3;
			double y2 = y1 + 5;

			if (m_direction == ListSortDirection.Ascending) {
				(y1, y2) = (y2, y1);
			}

			PathSegmentCollection pathSegmentCollection =
			[
				new LineSegment(new Point(x2, y1), true),
				new LineSegment(new Point(x3, y2), true)
			];

			var pathFigure = new PathFigure(
				new Point(x1, y1),
				pathSegmentCollection,
				true);

			PathFigureCollection pathFigureCollection =
			[
				pathFigure
			];

			var pathGeometry = new PathGeometry(pathFigureCollection);
			return pathGeometry;
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);

			if (m_sortGlyph != null) {
				double x    = m_columnHeader.ActualWidth - 13;
				double y    = m_columnHeader.ActualHeight / 2 - 5;
				var    rect = new Rect(x, y, 10, 10);
				drawingContext.DrawImage(m_sortGlyph, rect);
			}
			else {
				drawingContext.DrawGeometry(Brushes.LightGray, new Pen(Brushes.Gray, 1.0), GetDefaultGlyph());
			}
		}

	}

	#endregion

}