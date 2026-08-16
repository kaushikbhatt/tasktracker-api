using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskTracker.Application.DTOs;

/// <summary>
/// Distinguishes "the client did not mention this field" from "the client
/// explicitly set this field to null". Needed for JSON Merge Patch style
/// PATCH semantics 
/// </summary>
/// 
[JsonConverter(typeof(OptionalJsonConverterFactory))]
public readonly struct Optional<T>
{
	public bool IsSet { get; }
	public T? Value { get; }

	private Optional(bool isSet, T? value)
	{
		IsSet = isSet;
		Value = value;
	}

	public static Optional<T> Unset() => new(false, default);
	public static Optional<T> Of(T? value) => new(true, value);
}

public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert) =>
		typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var innerType = typeToConvert.GetGenericArguments()[0];
		var converterType = typeof(OptionalJsonConverter<>).MakeGenericType(innerType);
		return (JsonConverter)Activator.CreateInstance(converterType)!;
	}
}

public sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
	public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = JsonSerializer.Deserialize<T>(ref reader, options);
		return Optional<T>.Of(value);
	}

	public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
	{
		JsonSerializer.Serialize(writer, value.Value, options);
	}
}