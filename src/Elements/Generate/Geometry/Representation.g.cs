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
using Elements.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using Line = Elements.Geometry.Line;
using Polygon = Elements.Geometry.Polygon;

namespace Elements.Geometry
{
    #pragma warning disable // Disable all warnings

    /// <summary>The representation of an element.</summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.0.27.0 (Newtonsoft.Json v12.0.0.0)")]
    public partial class Representation 
    {
        [Newtonsoft.Json.JsonConstructor]
        public Representation(IList<SolidOperation> @solidOperations)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<Representation>();
            if(validator != null)
            {
                validator.Validate(new object[]{ @solidOperations});
            }
        
            this.SolidOperations = @solidOperations;
        }
    
        /// <summary>A collection of solid operations.</summary>
        [Newtonsoft.Json.JsonProperty("SolidOperations", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        public IList<SolidOperation> SolidOperations { get; set; } = new List<SolidOperation>();
    
    
    }
}