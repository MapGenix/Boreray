using System;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace DotSpatial.Controls.Extensions.Map
{
	public static class Loader
	{
		public static void TryLoadingCatalog(AggregateCatalog catalog, ComposablePartCatalog cat)
		{
			try
			{
				// We call Parts.Count simply to load the dlls in this directory, so that we can determine whether they will load properly.
				if (cat.Parts.Any())
					catalog.Catalogs.Add(cat);
			}
			catch (ReflectionTypeLoadException ex)
			{
				Type type = ex.Types[0];
				string typeAssembly;
				if (type != null)
					typeAssembly = type.Assembly.ToString();
				else
					typeAssembly = String.Empty;

				string message = String.Format("Skipping extension {0}. {1}", typeAssembly, ex.LoaderExceptions.First().Message);
				Trace.WriteLine(message);
				MessageBox.Show(message);
			}
		}
	}
}
