//----------------------
// <auto-generated>
//     Generated using the NJsonSchema v10.0.27.0 (Newtonsoft.Json v12.0.0.0) (http://NJsonSchema.org)
// </auto-generated>
//----------------------
using Elements;
using Elements.GeoJSON;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elements.Geometry.Solids
{
    #pragma warning disable // Disable all warnings

    /// <summary>A sweep of a profile along a curve.</summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.0.27.0 (Newtonsoft.Json v12.0.0.0)")]
    public partial class Sweep : SolidOperation
    {
        /// <summary>The id of the profile to be swept along the curve.</summary>
        [Newtonsoft.Json.JsonProperty("Profile", Required = Newtonsoft.Json.Required.AllowNull)]
        public Profile Profile { get;  set; }
    
        /// <summary>The curve along which the profile will be swept.</summary>
        [Newtonsoft.Json.JsonProperty("Curve", Required = Newtonsoft.Json.Required.AllowNull)]
        public Curve Curve { get;  set; }
    
        /// <summary>The amount to set back the resulting solid from the start of the curve.</summary>
        [Newtonsoft.Json.JsonProperty("StartSetback", Required = Newtonsoft.Json.Required.Always)]
        public double StartSetback { get;  set; }
    
        /// <summary>The amount to set back the resulting solid from the end of the curve.</summary>
        [Newtonsoft.Json.JsonProperty("EndSetback", Required = Newtonsoft.Json.Required.Always)]
        public double EndSetback { get;  set; }
    
    
    }
}