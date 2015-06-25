using System;
using DotSpatial.Data;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using ClassCleanup = NUnit.Framework.TestFixtureTearDownAttribute;
using ClassInitialize = NUnit.Framework.TestFixtureSetUpAttribute;
using Assert = NUnit.Framework.Assert;
using System.IO;
using DotSpatial.Serialization;
using DotSpatial.Projections;
using System.Collections.Generic;
using DotSpatial.Symbology;

namespace DotSpatial.Controls.Tests
{
    
    [TestClass]
    public class MapTest
    {
        [TestMethod]
        public void ZoomToMaxExtentTest()
        {
            XmlDeserializer target = new XmlDeserializer();
            Map map = new Map();
            string path = Path.Combine("TestFiles", "testproject1.dspx");

            target.Deserialize(map, File.ReadAllText(path));

            map.ZoomToMaxExtent();
        }

        [TestMethod]
        public void DefaultProjectionIsWgs84Test()
        {
            Map map = new Map();
            Assert.IsNotNull(map.Projection);
            Assert.AreEqual(map.Projection, KnownCoordinateSystems.Geographic.World.WGS1984);
        }

    
		[TestMethod]
        public void ProjectionChangedEventFireTest()
        {
            bool eventIsFired = false;
            
            Map map = new Map();
            map.ProjectionChanged += delegate {
                eventIsFired = true;
            };

            const string esri = "GEOGCS[\"GCS_North_American_1983\",DATUM[\"D_North_American_1983\",SPHEROID[\"GRS_1980\",6378137,298.257222101004]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.0174532925199433]]";
            map.ProjectionEsriString = esri;

            Assert.IsTrue(eventIsFired, "the ProjectionChanged event should be fired when Map.ProjectionEsriString is changed.");
        }

        
		[TestMethod]
        public void GetAllLayersTest()
        {
			var map = CreateMapWithNestedGroup();
			List<ILayer> layerList = map.GetAllLayers();
            Assert.AreEqual(layerList.Count, 6);
        }

        [TestMethod]
        public void GetAllGroupsTest()
        {
            Map map = CreateMapWithNestedGroup();
	        List<IMapGroup> groupList = map.GetAllGroups();
            Assert.AreEqual(groupList.Count, 2);
        }

	    private static Map CreateMapWithNestedGroup()
	    {
			Map map = new Map();
		    MapGroup group = CreateMapGroup();
		    map.Layers.Add(group);
		    group.Layers.Add(CreateMapGroup());
		    return map;
	    }

	    private static MapGroup CreateMapGroup()
	    {
		    var group = new MapGroup();
		    group.Layers.Add(new MapPolygonLayer());
		    group.Layers.Add(new MapLineLayer());
		    group.Layers.Add(new MapPointLayer());
		    return group;
	    }
    }
}