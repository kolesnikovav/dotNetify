﻿/* 
Copyright 2016 Dicky Suryadi

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DotNetify
{
   /// <summary>
   /// Custom view model resolver for JSON serializer.
   /// </summary>
   public class VMContractResolver : DefaultContractResolver
   {
      internal List<string> IgnoredPropertyNames { get; set; }

      /// <summary>
      /// Converter for properties of ICommand type which simply serializes the value to null.
      /// </summary>
      protected class CommandConverter : JsonConverter
      {
         /// <summary>
         /// Determines whether this instance can convert the specified object type.
         /// </summary>
         public override bool CanConvert(Type objectType) => typeof(ICommand).GetTypeInfo().IsAssignableFrom(objectType);

         /// <summary>
         /// Reads the JSON representation of the object; not used.
         /// </summary>
         public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();

         /// <summary>
         /// Writes null value to the JSON output.
         /// </summary>
         public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => writer.WriteNull();
      }

      /// <summary>
      /// Converter for properties of ReactiveProperty type which serializes the property value.
      /// </summary>
      protected class ReactivePropertyConverter : JsonConverter
      {
         /// <summary>
         /// Determines whether this instance can convert the specified object type.
         /// </summary>
         public override bool CanConvert(Type objectType) => objectType.GetTypeInfo().IsGenericType && typeof(IReactiveProperty).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo().GetGenericTypeDefinition());

         /// <summary>
         /// Reads the JSON representation of the object; not used.
         /// </summary>
         public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();

         /// <summary>
         /// Writes the property value to the JSON output.
         /// </summary>
         public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => serializer.Serialize(writer, (value as IReactiveProperty).Value);
      }

      /// <summary>
      /// Overrides this method to exclude properties with [Ignore] attribute or those that are in the given list.
      /// </summary>
      protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
      {
         var property = base.CreateProperty(member, memberSerialization);

         // Don't serialize properties that are decorated with [Ignore] or whose name are in the given list.
         if (member.GetCustomAttribute(typeof(IgnoreAttribute)) != null)
            property.Ignored = true;
         else if (IgnoredPropertyNames != null && IgnoredPropertyNames.Contains(property.PropertyName))
            property.Ignored = true;

         return property;
      }

      /// <summary>
      /// Overrides this method to prevent serialization of ICommand or Action property type.
      /// </summary>
      protected override JsonConverter ResolveContractConverter(Type objectType)
      {
         if (typeof(ICommand).GetTypeInfo().IsAssignableFrom(objectType))
            return new CommandConverter();
         else if (typeof(IReactiveProperty).GetTypeInfo().IsAssignableFrom(objectType))
            return new ReactivePropertyConverter();
         else
         {
            var typeInfo = objectType.GetTypeInfo();
            if (typeInfo.IsSubclassOf(typeof(MulticastDelegate)) && typeInfo.GetMethod(nameof(Action.Invoke)).ReturnType == typeof(void))
               return new CommandConverter();
         }
         return base.ResolveContractConverter(objectType);
      }
   }
}
