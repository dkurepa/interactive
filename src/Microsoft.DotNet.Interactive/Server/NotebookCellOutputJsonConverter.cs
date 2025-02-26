﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text.Json.Serialization;


namespace Microsoft.DotNet.Interactive.Server
{
    //public class NotebookCellOutputJsonConverter : JsonConverter
    //{
    //    public override bool CanRead { get; } = true;
    //    public override bool CanWrite { get; } = false;

    //    public override bool CanConvert(Type objectType)
    //    {
    //        // only convert the base type, otherwise we infinitely recurse when parsing in `ReadJson`
    //        return objectType == typeof(InteractiveDocumentOutputElement);
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        // cell outputs can be differentiated by their shape
    //        var item = JObject.Load(reader);
    //        if (HasAllProperties(item, "data"))
    //        {
    //            return item.ToObject<DisplayElement>(serializer);
    //        }

    //        if (HasAllProperties(item, "errorName", "errorValue", "stackTrace"))
    //        {
    //            return item.ToObject<ErrorElement>(serializer);
    //        }

    //        if (HasAllProperties(item, "text"))
    //        {
    //            return item.ToObject<TextElement>(serializer);
    //        }

    //        return null;
    //    }

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private static bool HasAllProperties(JToken token, params string[] propertyNames)
    //    {
    //        return propertyNames.All(propertyName => token[propertyName] is { });
    //    }
    //}
}
