using System.Text.Json.Serialization;

namespace App.Application.Contracts.Prediction;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "requestType")]
[JsonDerivedType(typeof(OrderReferencePredictionRequest), "orderReference")]
[JsonDerivedType(typeof(WhatIfPredictionRequest), "whatIf")]
public abstract record PredictionRequest;
