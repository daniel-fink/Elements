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

    /// <summary>A profile comprised of an external boundary and one or several holes.</summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.0.27.0 (Newtonsoft.Json v12.0.0.0)")]
    public partial class Profile : Identifiable
    {
        /// <summary>The perimeter of the profile.</summary>
        [Newtonsoft.Json.JsonProperty("Perimeter", Required = Newtonsoft.Json.Required.AllowNull)]
        public Polygon Perimeter { get;  set; }
    
        /// <summary>A collection of Polygons representing voids in the profile.</summary>
        [Newtonsoft.Json.JsonProperty("Voids", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public IList<Polygon> Voids { get;  set; }
    
    
    }
}