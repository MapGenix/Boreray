using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotSpatial.Controls.Extensions;
using DotSpatial.Data;
using DotSpatial.Topology;
using NUnit.Framework;

namespace DotSpatial.Controls.Tests
{
	
	public class PointLayerHelperTest
	{
		[Test]
		public void CreateDrawListFromVerts()
		{
			List<Extent> regions = new List<Extent>
			{
				new Extent(0, 0, 5, 5),
				new Extent(10, 10, 15, 15),
			};
			
			double[] verts = {0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17};
			List<int>list = PointLayerHelper.CreateDrawListFromVerts(regions, verts);
			Assert.AreEqual(6, list.Count);
			Assert.AreEqual(0, list[0]);
			Assert.AreEqual(1, list[1]);
			Assert.AreEqual(2, list[2]);
			Assert.AreEqual(5, list[3]);
			Assert.AreEqual(6, list[4]);
			Assert.AreEqual(7, list[5]);
		}

		[Test]
		public void CreateDrawListFromShape()
		{
			List<Extent> regions = new List<Extent>
			{
				new Extent(0, 0, 5, 5),
				new Extent(10, 10, 15, 15),
			};
			List<ShapeRange> shapes = new List<ShapeRange>();
			ShapeRange range = new ShapeRange(FeatureType.Point, CoordinateType.Regular);
			double[] verts = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 };
			range.SetVertices(verts);
			shapes.Add(range);

			List<int> list = PointLayerHelper.CreateDrawListFromShape(regions, shapes);
			Assert.AreEqual(0, list.Count);
			
		}
	}
}
