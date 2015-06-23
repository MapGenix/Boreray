// ********************************************************************************************************
// Product Name: DotSpatial.Controls.dll
// Description:  The Windows Forms user interface controls like the map, legend, toolbox, ribbon and others.
// ********************************************************************************************************
// The contents of this file are subject to the MIT License (MIT)
// you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://dotspatial.codeplex.com/license
//
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF
// ANY KIND, either expressed or implied. See the License for the specific language governing rights and
// limitations under the License.
//
// The Original Code is from MapWindow.dll version 6.0
// ********************************************************************************************************

using DotSpatial.Controls.Extensions;
using DotSpatial.Data;
using DotSpatial.Symbology;
using DotSpatial.Topology;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace DotSpatial.Controls
{
	/// <summary>
	/// GeoLabelLayer
	/// </summary>
	public class MapLabelLayer : LabelLayer, IMapLabelLayer
	{
		#region Events

		/// <summary>
		/// Fires an event that indicates to the parent map-frame that it should first
		/// redraw the specified clip
		/// </summary>
		public event EventHandler<ClipArgs> BufferChanged;

		#endregion Events

		#region Private Variables

		/// <summary>
		/// The existing labels, accessed for all map label layers, not just this instance
		/// </summary>
		private static readonly List<RectangleF> ExistingLabels = new List<RectangleF>(); // for collision prevention, tracks existing labels.

		private static readonly Caches Caches = new Caches();

		#endregion Private Variables

		#region Constructors

		/// <summary>
		/// Creates a new instance of GeoLabelLayer
		/// </summary>
		public MapLabelLayer()
		{
			Configure();
		}

		/// <summary>
		/// Creates a new label layer based on the specified featureset
		/// </summary>
		/// <param name="inFeatureSet"></param>
		public MapLabelLayer(IFeatureSet inFeatureSet)
			: base(inFeatureSet)
		{
			Configure();
		}

		/// <summary>
		/// Creates a new label layer based on the specified feature layer
		/// </summary>
		/// <param name="inFeatureLayer">The feature layer to build layers from</param>
		public MapLabelLayer(IFeatureLayer inFeatureLayer)
			: base(inFeatureLayer)
		{
			Configure();
		}

		private void Configure()
		{
			ChunkSize = 10000;
		}

		#endregion Constructors

		#region Methods

		/// <summary>
		/// Cleaer all existing labels for all layers
		/// </summary>
		public static void ClearAllExistingLabels()
		{
			ExistingLabels.Clear();
		}

		/// <summary>
		/// This will draw any features that intersect this region.  To specify the features
		/// directly, use OnDrawFeatures.  This will not clear existing buffer content.
		/// For that call Initialize instead.
		/// </summary>
		/// <param name="args">A GeoArgs clarifying the transformation from geographic to image space</param>
		/// <param name="regions">The geographic regions to draw</param>
		public void DrawRegions(MapArgs args, List<Extent> regions)
		{
			if (FeatureSet == null) return;

			if (FeatureSet.IndexMode)
			{
				List<int> drawIndices = CreateIndiceList(regions);
				List<Rectangle> clips = args.ProjToPixel(regions);
				DrawFeatures(args, drawIndices, clips, true);
			}
			else
			{
				List<IFeature> drawList = CreateFeatureList(regions);
				List<Rectangle> clipRects = args.ProjToPixel(regions);
				DrawFeatures(args, drawList, clipRects, true);
			}
		}

		/// <summary>
		/// Call StartDrawing before using this.
		/// </summary>
		/// <param name="rectangles">The rectangular region in pixels to clear.</param>
		/// <param name= "color">The color to use when clearing.  Specifying transparent
		/// will replace content with transparent pixels.</param>
		public void Clear(List<Rectangle> rectangles, Color color)
		{
			if (BackBuffer == null) return;
			Graphics g = Graphics.FromImage(BackBuffer);
			foreach (Rectangle r in rectangles)
			{
				if (r.IsEmpty == false)
				{
					g.Clip = new Region(r);
					g.Clear(color);
				}
			}
			g.Dispose();
		}

		/// <summary>
		/// If useChunks is true, then this method
		/// </summary>
		/// <param name="args">The GeoArgs that control how these features should be drawn.</param>
		/// <param name="features">The features that should be drawn.</param>
		/// <param name="clipRectangles">If an entire chunk is drawn and an update is specified, this clarifies the changed rectangles.</param>
		/// <param name="useChunks">Boolean, if true, this will refresh the buffer in chunks.</param>
		public virtual void DrawFeatures(MapArgs args, List<IFeature> features, List<Rectangle> clipRectangles, bool useChunks)
		{
			if (useChunks == false)
			{
				DrawFeatures(args, features);
				return;
			}

			int count = features.Count;
			int numChunks = (int)Math.Ceiling(count / (double)ChunkSize);

			for (int chunk = 0; chunk < numChunks; chunk++)
			{
				int numFeatures = ChunkSize;
				if (chunk == numChunks - 1) numFeatures = features.Count - (chunk * ChunkSize);
				DrawFeatures(args, features.GetRange(chunk * ChunkSize, numFeatures));

				if (numChunks > 0 && chunk < numChunks - 1)
				{
					//  FinishDrawing();
					OnBufferChanged(clipRectangles);
					Application.DoEvents();
					//this.StartDrawing();
				}
			}
		}

		/// <summary>
		/// If useChunks is true, then this method
		/// </summary>
		/// <param name="args">The GeoArgs that control how these features should be drawn.</param>
		/// <param name="features">The features that should be drawn.</param>
		/// <param name="clipRectangles">If an entire chunk is drawn and an update is specified, this clarifies the changed rectangles.</param>
		/// <param name="useChunks">Boolean, if true, this will refresh the buffer in chunks.</param>
		public virtual void DrawFeatures(MapArgs args, List<int> features, List<Rectangle> clipRectangles, bool useChunks)
		{
			if (useChunks == false)
			{
				DrawFeatures(args, features);
				return;
			}

			int count = features.Count;
			int numChunks = (int)Math.Ceiling(count / (double)ChunkSize);

			for (int chunk = 0; chunk < numChunks; chunk++)
			{
				int numFeatures = ChunkSize;
				if (chunk == numChunks - 1) numFeatures = features.Count - (chunk * ChunkSize);
				DrawFeatures(args, features.GetRange(chunk * ChunkSize, numFeatures));

				if (numChunks > 0 && chunk < numChunks - 1)
				{
					//  FinishDrawing();
					OnBufferChanged(clipRectangles);
					Application.DoEvents();
					//this.StartDrawing();
				}
			}
		}

		// This draws the individual line features
		private void DrawFeatures(MapArgs e, IEnumerable<int> features)
		{
			// Check that exists at least one category with Expression
			if (Symbology.Categories.All(_ => string.IsNullOrEmpty(_.Expression)))
				return;

			Graphics g = e.Device ?? Graphics.FromImage(BackBuffer);

			// Only draw features that are currently visible.

			if (FastDrawnStates == null)
			{
				CreateIndexedLabels();
			}
			FastLabelDrawnState[] drawStates = FastDrawnStates;
			if (drawStates == null)
				return;

			g.TextRenderingHint = TextRenderingHint.AntiAlias;
			g.SmoothingMode = SmoothingMode.AntiAlias;

			Action<int, IFeature> drawPolygon = (fid, feature) => DrawPolygonFeature(e, g, feature, drawStates[fid].Category, drawStates[fid].Selected, ExistingLabels);
			Action<int, IFeature> drawLine = (fid, feature) => DrawLineFeature(e, g, feature, drawStates[fid].Category, drawStates[fid].Selected, ExistingLabels);
			Action<int, IFeature> drawPoint = (fid, feature) => DrawPointFeature(e, g, feature, drawStates[fid].Category, drawStates[fid].Selected, ExistingLabels);

			Action<int, IFeature> drawFeature;
			switch (FeatureSet.FeatureType)
			{
				case FeatureType.Polygon:
					drawFeature = drawPolygon;
					break;

				case FeatureType.Line:
					drawFeature = drawLine;
					break;

				case FeatureType.Point:
					drawFeature = drawPoint;
					break;

				default:
					return; // Can't draw something else
			}

			foreach (var category in Symbology.Categories)
			{
				List<int> catFeatures = FilterCategoryFeatures(features, drawStates, category);

				Prioritize(category, catFeatures);

				foreach (var fid in catFeatures)
				{
					if (!FeatureLayer.DrawnStates[fid].Visible) continue;
					var feature = FeatureSet.GetFeature(fid);
					drawFeature(fid, feature);
				}
			}

			if (e.Device == null) g.Dispose();
		}

		private static void Prioritize(ILabelCategory category, List<int> catFeatures)
		{
			if (category.Symbolizer.PriorityField != "FID")
			{
				Feature.ComparisonField = category.Symbolizer.PriorityField;
				catFeatures.Sort();
				// When preventing collisions, we want to do high priority first.
				// otherwise, do high priority last.
				if (category.Symbolizer.PreventCollisions)
				{
					if (!category.Symbolizer.PrioritizeLowValues)
					{
						catFeatures.Reverse();
					}
				}
				else
				{
					if (category.Symbolizer.PrioritizeLowValues)
					{
						catFeatures.Reverse();
					}
				}
			}
		}

		private static List<int> FilterCategoryFeatures(IEnumerable<int> features, FastLabelDrawnState[] drawStates,
			ILabelCategory category)
		{
			var catFeatures = new List<int>();
			foreach (int fid in features)
			{
				if (drawStates[fid] == null || drawStates[fid].Category == null) continue;
				if (drawStates[fid].Category == category)
				{
					catFeatures.Add(fid);
				}
			}
			return catFeatures;
		}

		// This draws the individual line features
		private void DrawFeatures(MapArgs e, IEnumerable<IFeature> features)
		{
			// Check that exists at least one category with Expression
			if (Symbology.Categories.All(_ => string.IsNullOrEmpty(_.Expression))) return;

			Graphics g = e.Device ?? Graphics.FromImage(BackBuffer);

			// Only draw features that are currently visible.
			if (DrawnStates == null || !DrawnStates.ContainsKey(features.First()))
			{
				CreateLabels();
			}
			Dictionary<IFeature, LabelDrawState> drawStates = DrawnStates;
			if (drawStates == null)
				return;
			g.TextRenderingHint = TextRenderingHint.AntiAlias;
			g.SmoothingMode = SmoothingMode.AntiAlias;

			Action<IFeature> drawPolygon = f => DrawPolygonFeature(e, g, f, drawStates[f].Category, drawStates[f].Selected, ExistingLabels);
			Action<IFeature> drawLine = f => DrawLineFeature(e, g, f, drawStates[f].Category, drawStates[f].Selected, ExistingLabels);
			Action<IFeature> drawPoint = f => DrawPointFeature(e, g, f, drawStates[f].Category, drawStates[f].Selected, ExistingLabels);

			Action<IFeature> drawFeature;
			switch (features.First().FeatureType)
			{
				case FeatureType.Polygon:
					drawFeature = drawPolygon;
					break;

				case FeatureType.Line:
					drawFeature = drawLine;
					break;

				case FeatureType.Point:
					drawFeature = drawPoint;
					break;

				default:
					return; // Can't draw something else
			}

			foreach (ILabelCategory category in Symbology.Categories)
			{
				var cat = category; // prevent access to unmodified closure problems
				var catFeatures = LabelLayerHelper.FilterCatFeatures(features, drawStates, cat);

				LabelLayerHelper.Prioritize(category, cat, catFeatures);
				for (int i = 0; i < catFeatures.Count; i++)
				{
					if (!FeatureLayer.DrawnStates[i].Visible)
						continue;
					drawFeature(catFeatures[i]);
				}
			}

			if (e.Device == null) g.Dispose();
		}

		/// <summary>
		/// Draws a label on a polygon with various different methods
		/// </summary>
		public static void DrawPolygonFeature(MapArgs e, Graphics g, IFeature f, ILabelCategory category, bool selected, List<RectangleF> existingLabels)
		{
			var symb = selected ? category.SelectionSymbolizer : category.Symbolizer;

			//Gets the features text and calculate the label size
			string txt = LabelLayerHelper.GetLabelText(f, category, symb);
			if (txt == null) return;
			Func<SizeF> labelSize = () => g.MeasureString(txt, Caches.GetFont(symb));

			if (f.NumGeometries == 1)
			{
				RectangleF labelBounds = PlacePolygonLabel(f.BasicGeometry, e, labelSize, symb);
				CollisionDraw(txt, g, symb, f, e, labelBounds, existingLabels);
			}
			else
			{
				if (symb.PartsLabelingMethod == PartLabelingMethod.LabelAllParts)
				{
					for (int n = 0; n < f.NumGeometries; n++)
					{
						RectangleF labelBounds = PlacePolygonLabel(f.GetBasicGeometryN(n), e, labelSize, symb);
						CollisionDraw(txt, g, symb, f, e, labelBounds, existingLabels);
					}
				}
				else
				{
					double largestArea = 0;
					IPolygon largest = null;
					for (int n = 0; n < f.NumGeometries; n++)
					{
						IPolygon pg = Geometry.FromBasicGeometry(f.GetBasicGeometryN(n)) as IPolygon;
						if (pg == null) continue;
						double tempArea = pg.Area;
						if (largestArea < tempArea)
						{
							largestArea = tempArea;
							largest = pg;
						}
					}
					RectangleF labelBounds = PlacePolygonLabel(largest, e, labelSize, symb);
					CollisionDraw(txt, g, symb, f, e, labelBounds, existingLabels);
				}
			}
		}

		private static void CollisionDraw(string txt, Graphics g, ILabelSymbolizer symb, IFeature f, MapArgs e, RectangleF labelBounds, List<RectangleF> existingLabels)
		{
			if (labelBounds.IsEmpty || !e.ImageRectangle.IntersectsWith(labelBounds)) return;
			if (symb.PreventCollisions)
			{
				if (!LabelLayerHelper.Collides(labelBounds, existingLabels))
				{
					DrawLabel(g, txt, labelBounds, symb, f);
					existingLabels.Add(labelBounds);
				}
			}
			else
			{
				DrawLabel(g, txt, labelBounds, symb, f);
			}
		}

		public static RectangleF PlacePolygonLabel(IBasicGeometry geom, MapArgs e, Func<SizeF> labelSize, ILabelSymbolizer symb)
		{
			IPolygon pg = Geometry.FromBasicGeometry(geom) as IPolygon;
			if (pg == null) return RectangleF.Empty;
			Coordinate c;
			switch (symb.LabelPlacementMethod)
			{
				case LabelPlacementMethod.Centroid:
					c = pg.Centroid.Coordinates[0];
					break;

				case LabelPlacementMethod.InteriorPoint:
					c = pg.InteriorPoint.Coordinate;
					break;

				default:
					c = geom.Envelope.Center();
					break;
			}
			if (e.GeographicExtents.Intersects(c) == false) return RectangleF.Empty;
			var lz = labelSize();
			PointF adjustment = LabelLayerHelper.Position(symb, lz);
			float x = Convert.ToSingle((c.X - e.MinX) * e.Dx) + adjustment.X;
			float y = Convert.ToSingle((e.MaxY - c.Y) * e.Dy) + adjustment.Y;
			return new RectangleF(x, y, lz.Width, lz.Height);
		}

		/// <summary>
		/// Draws a label on a point with various different methods
		/// </summary>
		/// <param name="e"></param>
		/// <param name="g"></param>
		/// <param name="f"></param>
		/// <param name="category"></param>
		/// <param name="selected"></param>
		/// <param name="existingLabels"></param>
		public static void DrawPointFeature(MapArgs e, Graphics g, IFeature f, ILabelCategory category, bool selected, List<RectangleF> existingLabels)
		{
			var symb = selected ? category.SelectionSymbolizer : category.Symbolizer;

			//Gets the features text and calculate the label size
			string txt = LabelLayerHelper.GetLabelText(f, category, symb);
			if (txt == null) return;

			Func<SizeF> labelSize = () => g.MeasureString(txt, Caches.GetFont(symb));

			//Depending on the labeling strategy we do diff things
			if (symb.PartsLabelingMethod == PartLabelingMethod.LabelAllParts)
			{
				for (int n = 0; n < f.NumGeometries; n++)
				{
					RectangleF labelBounds = LabelLayerHelper.PlacePointLabel(f, e, labelSize, symb);
					CollisionDraw(txt, g, symb, f, e, labelBounds, existingLabels);
				}
			}
			else
			{
				RectangleF labelBounds = LabelLayerHelper.PlacePointLabel(f, e, labelSize, symb);
				CollisionDraw(txt, g, symb, f, e, labelBounds, existingLabels);
			}
		}

		/// <summary>
		/// Draws a label on a line with various different methods
		/// </summary>
		public static void DrawLineFeature(MapArgs e, Graphics g, IFeature f, ILabelCategory category, bool selected, List<RectangleF> existingLabels)
		{
			var symb = selected ? category.SelectionSymbolizer : category.Symbolizer;

			//Gets the features text and calculate the label size
			string txt = LabelLayerHelper.GetLabelText(f, category, symb);
			if (txt == null) return;
			Func<SizeF> labelSize = () => g.MeasureString(txt, Caches.GetFont(symb));

			if (f.NumGeometries == 1)
			{
				RectangleF labelBounds = LabelLayerHelper.PlaceLineLabel(f.BasicGeometry, labelSize, e, symb);
				CollisionDraw(txt, g, symb, f, e, labelBounds, existingLabels);
			}
			else
			{
				//Depending on the labeling strategy we do diff things
				if (symb.PartsLabelingMethod == PartLabelingMethod.LabelAllParts)
				{
					for (int n = 0; n < f.NumGeometries; n++)
					{
						RectangleF labelBounds = LabelLayerHelper.PlaceLineLabel(f.GetBasicGeometryN(n), labelSize, e, symb);
						CollisionDraw(txt, g, symb, f, e, labelBounds, existingLabels);
					}
				}
				else
				{
					double longestLine = 0;
					int longestIndex = 0;
					for (int n = 0; n < f.NumGeometries; n++)
					{
						ILineString ls = f.GetBasicGeometryN(n) as ILineString;
						double tempLength = 0;
						if (ls != null) tempLength = ls.Length;
						if (longestLine < tempLength)
						{
							longestLine = tempLength;
							longestIndex = n;
						}
					}
					RectangleF labelBounds = LabelLayerHelper.PlaceLineLabel(f.GetBasicGeometryN(longestIndex), labelSize, e, symb);
					CollisionDraw(txt, g, symb, f, e, labelBounds, existingLabels);
				}
			}
		}

		/// <summary>
		/// Draws labels in a specified rectangle
		/// </summary>
		/// <param name="g">The graphics object to draw to</param>
		/// <param name="labelText">The label text to draw</param>
		/// <param name="labelBounds">The rectangle of the label</param>
		/// <param name="symb">the Label Symbolizer to use when drawing the label</param>
		/// <param name="feature">Feature to draw</param>
		private static void DrawLabel(Graphics g, string labelText, RectangleF labelBounds, ILabelSymbolizer symb, IFeature feature)
		{
			//Sets up the brushes and such for the labeling
			Font textFont = Caches.GetFont(symb);
			var format = new StringFormat { Alignment = symb.Alignment, };

			//Text graphics path
			var gp = new GraphicsPath();
			gp.AddString(labelText, textFont.FontFamily, (int)textFont.Style, textFont.SizeInPoints * 96F / 72F, labelBounds, format);

			// Rotate text
			var angleToRotate = LabelLayerHelper.GetAngleToRotate(symb, feature);
			GraphicsHelper.RotateAt(g, labelBounds.X, labelBounds.Y, angleToRotate);

			// Draws the text outline
			if (symb.BackColorEnabled && symb.BackColor != Color.Transparent)
			{
				var backBrush = Caches.GetSolidBrush(symb.BackColor);
				if (symb.FontColor == Color.Transparent)
				{
					using (var backgroundGp = new GraphicsPath())
					{
						backgroundGp.AddRectangle(labelBounds);
						backgroundGp.FillMode = FillMode.Alternate;
						backgroundGp.AddPath(gp, true);
						g.FillPath(backBrush, backgroundGp);
					}
				}
				else
				{
					g.FillRectangle(backBrush, labelBounds);
				}
			}

			// Draws the border if its enabled
			if (symb.BorderVisible && symb.BorderColor != Color.Transparent)
			{
				var borderPen = Caches.GetPen(symb.BorderColor);
				g.DrawRectangle(borderPen, labelBounds.X, labelBounds.Y, labelBounds.Width, labelBounds.Height);
			}

			// Draws the drop shadow
			if (symb.DropShadowEnabled && symb.DropShadowColor != Color.Transparent)
			{
				var shadowBrush = Caches.GetSolidBrush(symb.DropShadowColor);
				var gpTrans = new Matrix();
				gpTrans.Translate(symb.DropShadowPixelOffset.X, symb.DropShadowPixelOffset.Y);
				gp.Transform(gpTrans);
				g.FillPath(shadowBrush, gp);
				gpTrans = new Matrix();
				gpTrans.Translate(-symb.DropShadowPixelOffset.X, -symb.DropShadowPixelOffset.Y);
				gp.Transform(gpTrans);
				gpTrans.Dispose();
			}

			// Draws the text halo
			if (symb.HaloEnabled && symb.HaloColor != Color.Transparent)
			{
				using (var haloPen = new Pen(symb.HaloColor) { Width = 2, Alignment = PenAlignment.Outset })
					g.DrawPath(haloPen, gp);
			}

			// Draws the text if its not transparent
			if (symb.FontColor != Color.Transparent)
			{
				var foreBrush = Caches.GetSolidBrush(symb.FontColor);
				g.FillPath(foreBrush, gp);
			}
			gp.Dispose();
		}

		/// <summary>
		/// Indicates that the drawing process has been finalized and swaps the back buffer
		/// to the front buffer.
		/// </summary>
		public void FinishDrawing()
		{
			OnFinishDrawing();
			if (Buffer != null && Buffer != BackBuffer) Buffer.Dispose();
			Buffer = BackBuffer;
			FeatureLayer.Invalidate();
		}

		/// <summary>
		/// Copies any current content to the back buffer so that drawing should occur on the
		/// back buffer (instead of the fore-buffer).  Calling draw methods without
		/// calling this may cause exceptions.
		/// </summary>
		/// <param name="preserve">Boolean, true if the front buffer content should be copied to the back buffer
		/// where drawing will be taking place.</param>
		public void StartDrawing(bool preserve)
		{
			Bitmap backBuffer = new Bitmap(BufferRectangle.Width, BufferRectangle.Height);
			if (Buffer != null)
			{
				if (Buffer.Width == backBuffer.Width && Buffer.Height == backBuffer.Height)
				{
					if (preserve)
					{
						Graphics g = Graphics.FromImage(backBuffer);
						g.DrawImageUnscaled(Buffer, 0, 0);
					}
				}
			}
			if (BackBuffer != null && BackBuffer != Buffer) BackBuffer.Dispose();
			BackBuffer = backBuffer;
			OnStartDrawing();
		}

		#endregion Methods

		#region Properties

		/// <summary>
		/// Gets or sets the back buffer that will be drawn to as part of the initialization process.
		/// </summary>
		[ShallowCopy, Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Image BackBuffer { get; set; }

		/// <summary>
		/// Gets the current buffer.
		/// </summary>
		[ShallowCopy, Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Image Buffer { get; set; }

		/// <summary>
		/// Gets or sets the geographic region represented by the buffer
		/// Calling Initialize will set this automatically.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IEnvelope BufferEnvelope { get; set; }

		/// <summary>
		/// Gets or sets the rectangle in pixels to use as the back buffer.
		/// Calling Initialize will set this automatically.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Rectangle BufferRectangle { get; set; }

		/// <summary>
		/// Gets or sets the maximum number of labels that will be rendered before
		/// refreshing the screen.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int ChunkSize { get; set; }

		/// <summary>
		/// Gets or sets the MapFeatureLayer that this label layer is attached to.
		/// </summary>
		[ShallowCopy, Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new IMapFeatureLayer FeatureLayer
		{
			get { return base.FeatureLayer as IMapFeatureLayer; }
			set { base.FeatureLayer = value; }
		}

		/// <summary>
		/// Gets or sets whether or not this layer has been initialized.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new bool IsInitialized { get; set; }

		#endregion Properties

		/// <summary>
		/// Fires the OnBufferChanged event
		/// </summary>
		/// <param name="clipRectangles">The Rectangle in pixels</param>
		protected virtual void OnBufferChanged(List<Rectangle> clipRectangles)
		{
			var h = BufferChanged;
			if (h != null)
			{
				h(this, new ClipArgs(clipRectangles));
			}
		}

		/// <summary>
		/// Indiciates that whatever drawing is going to occur has finished and the contents
		/// are about to be flipped forward to the front buffer.
		/// </summary>
		protected virtual void OnFinishDrawing()
		{
		}

		/// <summary>
		/// Occurs when a new drawing is started, but after the BackBuffer has been established.
		/// </summary>
		protected virtual void OnStartDrawing()
		{
		}

		private List<IFeature> CreateFeatureList(List<Extent> regions)
		{
			// First determine the number of features we are talking about based on region.
			List<IFeature> drawList = new List<IFeature>();
			foreach (Extent region in regions)
			{
				if (region != null)
				{
					// We need to consider labels that go off the screen.  figure a region
					// that is larger.
					Extent r = LabelLayerHelper.CreateExpandedRegion(region);
					// Use union to prevent duplicates.  No sense in drawing more than we have to.
					drawList = drawList.Union(FeatureSet.Select(r)).ToList();
				}
			}
			return drawList;
		}

		private List<int> CreateIndiceList(List<Extent> regions)
		{
			// First determine the number of features we are talking about based on region.
			List<int> drawIndices = new List<int>();
			foreach (Extent region in regions)
			{
				if (region != null)
				{
					// We need to consider labels that go off the screen.  figure a region
					// that is larger.
					Extent sur = LabelLayerHelper.CreateExpandedRegion(region);
					// Use union to prevent duplicates.  No sense in drawing more than we have to.
					drawIndices = drawIndices.Union(FeatureSet.SelectIndices(sur)).ToList();
				}
			}
			return drawIndices;
		}
	}
}