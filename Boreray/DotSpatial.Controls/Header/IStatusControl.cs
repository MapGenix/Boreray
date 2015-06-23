// -----------------------------------------------------------------------
// <copyright file="IStatusControl.cs" company="DotSpatial Team">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using DotSpatial.Data;
using System.ComponentModel.Composition;

namespace DotSpatial.Controls.Header
{
	/// <summary>
	/// Used to display status information.
	/// </summary>
	[InheritedExport]
	public interface IStatusControl : IProgressHandler
	{
		/// <summary>
		/// Adds the specified panel.
		/// </summary>
		/// <param name="panel">The panel.</param>
		void Add(StatusPanel panel);

		/// <summary>
		/// Removes the specified panel.
		/// </summary>
		/// <param name="panel">The panel.</param>
		void Remove(StatusPanel panel);
	}
}