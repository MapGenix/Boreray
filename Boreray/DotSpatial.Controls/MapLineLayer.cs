// ********************************************************************************************************
// Product Name: DotSpatial.Controls.dll
// Description:  The core libraries for the DotSpatial project.
//
// ********************************************************************************************************
// The contents of this file are subject to the MIT License (MIT)
// you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://dotspatial.codeplex.com/license
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
using System.Linq;
using System.Windows.Forms;

namespace DotSpatial.Controls
{
	public class MapLineLayer : LineLayer, IMapLineLayer
	{
		/// <summary>
		/// Fires an event that indicates to the parent map-frame that it should first
		/// redraw the specified clip
		/// </summary>
		public event EventHandler<ClipArgs> BufferChanged;

		private static readonly Func<IDrawnState, ILineCategory, int, bool> IsMember = (state, category, selectState) =>
			   state.SchemeCategory == category &&
			   state.IsVisible &&
			   state.IsSelected == (selectState == 1);

		/// <summary>
		/// Creates an empty line layer with a Line FeatureSet that has no members.
		/// </summary>
		public MapLineLayer()
			: base(new FeatureSet(FeatureType.Line))
		{
			Configure();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="inFeatureSet"></param>
		public MapLineLayer(IFeatureSet inFeatureSet)
			: base(inFeatureSet)
		{
			Configure();
			OnFinishedLoading();
		}

		/// <summary>
		/// Constructor that also shows progress
		/// </summary>
		/// <param name="featureSet">A featureset that contains lines</param>
		/// <param name="container">An IContainer that the line layer should be created in</param>
		public MapLineLayer(IFeatureSet featureSet, ICollection<ILayer> container)
			: base(featureSet, container, null)
		{
			Configure();
			OnFinishedLoading();
		}

		/// <summary>
		/// Creates a GeoLineLayer constructor, but passes the boolean notFinished variable to indicate
		/// whether or not this layer should fire the FinishedLoading event.
		/// </summary>
		/// <param name="featureSet"></param>
		/// <param name="container"></param>
		/// <param name="notFinished"></param>
		public MapLineLayer(IFeatureSet featureSet, ICollection<ILayer> container, bool notFinished)
			: base(featureSet, container, null)
		{
			Configure();
			if (notFinished == false) OnFinishedLoading();
		}

		private void Configure()
		{
			BufferRectangle = new Rectangle(0, 0, 3000, 3000);
			ChunkSize = 50000;
		}

		/// <summary>
		/// This will draw any features that intersect this region.  To specify the features
		/// directly, use OnDrawFeatures.  This will not clear existing buffer content.
		/// For that call Initialize instead.
		/// </summary>
		/// <param name="args">A GeoArgs clarifying the transformation from geographic to image space</param>
		/// <param name="regions">The geographic regions to draw</param>
		public virtual void DrawRegions(MapArgs args, List<Extent> regions)
		{
			// First determine the number of features we are talking about based on region.
			List<Rectangle> clipRects = args.ProjToPixel(regions);
			if (EditMode)
			{
				List<IFeature> drawList = CreateFeatureList(regions);
				DrawFeatures(args, drawList, clipRects, true);
			}
			else
			{
				List<int> drawList = CreateIndiceList(regions);
				DrawFeatures(args, drawList, clipRects, true);
			}
		}

		private List<int> CreateIndiceList(List<Extent> regions)
		{
			List<int> drawList = new List<int>();
			List<ShapeRange> shapes = DataSet.ShapeIndices;
			for (int shp = 0; shp < shapes.Count; shp++)
			{
				foreach (Extent region in regions)
				{
					if (!shapes[shp].Extent.Intersects(region)) continue;
					drawList.Add(shp);
					break;
				}
			}
			return drawList;
		}

		private List<IFeature> CreateFeatureList(List<Extent> regions)
		{
			List<IFeature> drawList = new List<IFeature>();
			foreach (Extent region in regions)
			{
				if (region != null)
				{
					// Use union to prevent duplicates.  No sense in drawing more than we have to.
					drawList = drawList.Union(DataSet.Select(region)).ToList();
				}
			}
			return drawList;
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
		/// This is testing the idea of using an input parameter type that is marked as out
		/// instead of a return type.
		/// </summary>
		/// <param name="result">The result of the creation</param>
		/// <returns>Boolean, true if a layer can be created</returns>
		public override bool CreateLayerFromSelectedFeatures(out IFeatureLayer result)
		{
			MapLineLayer temp;
			bool resultOk = CreateLayerFromSelectedFeatures(out temp);
			result = temp;
			return resultOk;
		}

		/// <summary>
		/// This is the strong typed version of the same process that is specific to geo point layers.
		/// </summary>
		/// <param name="result">The new GeoPointLayer to be created</param>
		/// <returns>Boolean, true if there were any values in the selection</returns>
		public virtual bool CreateLayerFromSelectedFeatures(out MapLineLayer result)
		{
			result = null;
			if (Selection == null || Selection.Count == 0) return false;
			FeatureSet fs = Selection.ToFeatureSet();
			result = new MapLineLayer(fs);
			return true;
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
					OnBufferChanged(clipRectangles);
					Application.DoEvents();
				}
			}
		}

		/// <summary>
		/// If useChunks is true, then this method
		/// </summary>
		/// <param name="args">The GeoArgs that control how these features should be drawn.</param>
		/// <param name="indices">The features that should be drawn.</param>
		/// <param name="clipRectangles">If an entire chunk is drawn and an update is specified, this clarifies the changed rectangles.</param>
		/// <param name="useChunks">Boolean, if true, this will refresh the buffer in chunks.</param>
		public virtual void DrawFeatures(MapArgs args, List<int> indices, List<Rectangle> clipRectangles, bool useChunks)
		{
			if (useChunks == false)
			{
				DrawFeatures(args, indices);
				return;
			}

			int count = indices.Count;
			int numChunks = (int)Math.Ceiling(count / (double)ChunkSize);

			for (int chunk = 0; chunk < numChunks; chunk++)
			{
				int numFeatures = ChunkSize;
				if (chunk == numChunks - 1) numFeatures = indices.Count - (chunk * ChunkSize);
				DrawFeatures(args, indices.GetRange(chunk * ChunkSize, numFeatures));

				if (numChunks > 0 && chunk < numChunks - 1)
				{
					// FinishDrawing();
					OnBufferChanged(clipRectangles);
					Application.DoEvents();
					// this.StartDrawing();
				}
			}
		}

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
		[ShallowCopy, Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IEnvelope BufferEnvelope { get; set; }

		/// <summary>
		/// Gets or sets the rectangle in pixels to use as the back buffer.
		/// Calling Initialize will set this automatically.
		/// </summary>
		[ShallowCopy, Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Rectangle BufferRectangle { get; set; }

		/// <summary>
		/// Gets an integer number of chunks for this layer.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int NumChunks
		{
			get
			{
				if (DrawingFilter == null) return 0;
				return DrawingFilter.NumChunks;
			}
		}

		/// <summary>
		/// Gets or sets the label layer that is associated with this line layer.
		/// </summary>
		[ShallowCopy]
		public new IMapLabelLayer LabelLayer
		{
			get { return base.LabelLayer as IMapLabelLayer; }
			set { base.LabelLayer = value; }
		}

		/// <summary>
		/// Fires the OnBufferChanged event
		/// </summary>
		/// <param name="clipRectangles">The Rectangle in pixels</param>
		protected virtual void OnBufferChanged(List<Rectangle> clipRectangles)
		{
			if (BufferChanged != null)
			{
				ClipArgs e = new ClipArgs(clipRectangles);
				BufferChanged(this, e);
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
		/// A default method to generate a label layer.
		/// </summary>
		protected override void OnCreateLabels()
		{
			LabelLayer = new MapLabelLayer(this);
		}

		/// <summary>
		/// Occurs when a new drawing is started, but after the BackBuffer has been established.
		/// </summary>
		protected virtual void OnStartDrawing()
		{
		}

		private void DrawFeatures(MapArgs e, IEnumerable<int> indices)
		{
			Graphics g = e.Device ?? Graphics.FromImage(BackBuffer);

			if (DrawnStatesNeeded)
			{
				FastDrawnState[] states = DrawnStates;
				int max = indices.Max();
				if (max >= states.Length)
				{
					AssignFastDrawnStates();
					states = DrawnStates;
				}
				for (int selectState = 0; selectState < 2; selectState++)
				{
					foreach (ILineCategory category in Symbology.Categories)
					{
						ILineSymbolizer ls = LineLayerHelper.CreateLineSymbolizer(category, selectState);
						g.SmoothingMode = ls.Smoothing ? SmoothingMode.AntiAlias : SmoothingMode.None;

						Rectangle clipRect = LineLayerHelper.ComputeClippingRectangle(e, ls);

						List<int> drawnFeatures = FilterFeaturesToDraw(indices, states, category, selectState);

						GraphicsPath graphPath = CreateGraphPath(e, drawnFeatures, clipRect);
						double scale = LineLayerHelper.DefineScale(e, ls);

						foreach (IStroke stroke in ls.Strokes)
						{
							stroke.DrawPath(g, graphPath, scale);
						}

						graphPath.Dispose();
					}
				}
			}
			else
			{
				// Selection state is disabled
				// Category is only the very first category
				ILineCategory category = Symbology.Categories[0];
				ILineSymbolizer ls = category.Symbolizer;

				g.SmoothingMode = ls.Smoothing ? SmoothingMode.AntiAlias : SmoothingMode.None;

				Rectangle clipRect = LineLayerHelper.ComputeClippingRectangle(e, ls);

				GraphicsPath graphPath = CreateGraphicsPath(e, indices, clipRect);
				double scale = LineLayerHelper.DefineScale(e, ls);

				foreach (IStroke stroke in ls.Strokes)
				{
					stroke.DrawPath(g, graphPath, scale);
				}

				graphPath.Dispose();
			}

			if (e.Device == null) g.Dispose();
		}

		private GraphicsPath CreateGraphicsPath(MapArgs e, IEnumerable<int> indices, Rectangle clipRect)
		{
			GraphicsPath graphPath = new GraphicsPath();
			foreach (int shp in indices)
			{
				ShapeRange shape = DataSet.ShapeIndices[shp];
				LineLayerHelper.BuildLineString(graphPath, DataSet.Vertex, shape, e, clipRect);
			}
			return graphPath;
		}

		private GraphicsPath CreateGraphPath(MapArgs e, List<int> drawnFeatures, Rectangle clipRect)
		{
			GraphicsPath graphPath = new GraphicsPath();
			foreach (int shp in drawnFeatures)
			{
				ShapeRange shape = DataSet.ShapeIndices[shp];
				LineLayerHelper.BuildLineString(graphPath, DataSet.Vertex, shape, e, clipRect);
			}
			return graphPath;
		}

		private static List<int> FilterFeaturesToDraw(IEnumerable<int> indices, FastDrawnState[] states, ILineCategory lineCategory, int selectedIndice)
		{
			List<int> drawnFeatures = new List<int>();

			foreach (int index in indices)
			{
				FastDrawnState state = states[index];
				if (state.Category == lineCategory && state.Selected == (selectedIndice == 1) && state.Visible)
				{
					drawnFeatures.Add(index);
				}
			}
			return drawnFeatures;
		}

		// This draws the individual line features
		private void DrawFeatures(MapArgs e, IEnumerable<IFeature> features)
		{
			Graphics g = e.Device ?? Graphics.FromImage(BackBuffer);

			for (int selectState = 0; selectState < 2; selectState++)
			{
				foreach (ILineCategory category in Symbology.Categories)
				{
					ILineSymbolizer ls = LineLayerHelper.CreateLineSymbolizer(category, selectState);

					g.SmoothingMode = ls.Smoothing ? SmoothingMode.AntiAlias : SmoothingMode.None;

					Rectangle clipRect = LineLayerHelper.ComputeClippingRectangle(e, ls);

					var drawnFeatures = FilterFeaturesToDraw(features, category, selectState);

					DrawPath(e, drawnFeatures, clipRect, ls, g);
				}
			}
			if (e.Device == null)
				g.Dispose();
		}

		private void DrawPath(MapArgs e, IEnumerable<IFeature> drawnFeatures, Rectangle clipRect, ILineSymbolizer ls, Graphics g)
		{
			var graphPath = CreateGraphPath(e, drawnFeatures, clipRect);
			var scale = LineLayerHelper.DefineScale(e, ls);

			foreach (IStroke stroke in ls.Strokes)
			{
				stroke.DrawPath(g, graphPath, scale);
			}

			graphPath.Dispose();
		}

		private GraphicsPath CreateGraphPath(MapArgs e, IEnumerable<IFeature> drawnFeatures, Rectangle clipRect)
		{
			GraphicsPath graphPath = new GraphicsPath();
			foreach (IFeature f in drawnFeatures)
			{
				LineLayerHelper.BuildLineString(graphPath, DataSet.Vertex, f.ShapeIndex, e, clipRect);
			}
			return graphPath;
		}

		private IEnumerable<IFeature> FilterFeaturesToDraw(IEnumerable<IFeature> features, ILineCategory category, int selectState)
		{
			var drawnFeatures = from feature in features
								where IsMember(DrawingFilter[feature], category, selectState)
								select feature;
			return drawnFeatures;
		}
	}
}