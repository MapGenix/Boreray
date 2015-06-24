using System;
using System.IO;
using System.Linq;
using DotSpatial.Modeling.Forms;
using NUnit.Framework;

namespace DotSpatial.Controls.Tests
{
    /// <summary>
    ///This is a test class for AppManagerTest and is intended
    ///to contain all AppManagerTest Unit Tests
    ///</summary>
    [TestFixture()]
    public class AppManagerTest
    {
        [Test]
        public void GetCustomSettingDefaultTest()
        {
            Map map = new Map();
            AppManager target = new AppManager();
            target.Map = map;

            string uniqueName = "customsettingname";
            var expected = DateTime.Now;
            var actual = target.SerializationManager.GetCustomSetting(uniqueName, expected);
            Assert.AreEqual(expected, actual);
        }

        
        [Test]
        public void GetCustomSettingFromMemoryTest()
        {
            Map map = new Map();
            AppManager target = new AppManager();
            target.Map = map;

            string uniqueName = "customsettingname";
            var expected = DateTime.Now;
            target.SerializationManager.SetCustomSetting(uniqueName, expected);

            var actual = target.SerializationManager.GetCustomSetting(uniqueName, DateTime.Now.AddDays(1));
            Assert.AreEqual(expected, actual);

        }

       
        [Test]
        public void GetCustomSettingFromFileTest()
        {
            Map map = new Map();
            AppManager target = new AppManager();
            target.Map = map;

            string uniqueName = "customsettingname";
            var expected = DateTime.Now;
            target.SerializationManager.SetCustomSetting(uniqueName, expected);

            var actual = target.SerializationManager.GetCustomSetting(uniqueName, DateTime.Now.AddDays(1));
            Assert.AreEqual(expected, actual);

            string path = Path.GetFullPath(Path.Combine("TestFiles", "SerializeTestWithCustomSettings.map.xml.dspx"));

            target.SerializationManager.SaveProject(path);

            target.SerializationManager.OpenProject(path);
            actual = target.SerializationManager.GetCustomSetting(uniqueName, DateTime.Now.AddDays(1));
            Assert.AreEqual(expected.ToLongDateString(), actual.ToLongDateString());

            File.Delete(path);
        }

	   
    }
}
