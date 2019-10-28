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

namespace Elements.Geometry
{
    #pragma warning disable // Disable all warnings

    /// <summary>A 3D vector.</summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.0.27.0 (Newtonsoft.Json v12.0.0.0)")]
    public partial class Vector3 
    {
        /// <summary>The X component of the vector.</summary>
        [Newtonsoft.Json.JsonProperty("X", Required = Newtonsoft.Json.Required.Always)]
        public double X { get;  set; }
    
        /// <summary>The Y component of the vector.</summary>
        [Newtonsoft.Json.JsonProperty("Y", Required = Newtonsoft.Json.Required.Always)]
        public double Y { get;  set; }
    
        /// <summary>The Z component of the vector.</summary>
        [Newtonsoft.Json.JsonProperty("Z", Required = Newtonsoft.Json.Required.Always)]
        public double Z { get;  set; }
    
    
    }
}