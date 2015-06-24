using System;

namespace DotSpatial.Controls.Extensions.Map
{
    public class MapChangedEventArgs : EventArgs
    {
    	public IMap OldValue { get; set; }
    
    	public IMap NewValue { get; set; }
    
    	public MapChangedEventArgs(IMap oldValue, IMap newValue)
    	{
    		OldValue = oldValue;
    		NewValue = newValue;
    	}
    }
}
