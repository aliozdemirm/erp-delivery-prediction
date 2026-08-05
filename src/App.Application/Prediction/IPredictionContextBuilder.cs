using App.Application.Contracts.Erp;
using App.Domain.Prediction;

namespace App.Application.Prediction;

public interface IPredictionContextBuilder
{
    (DataSufficiency Status, PredictionContext? Context) Build(ErpBatchSnapshot snapshot);
}
