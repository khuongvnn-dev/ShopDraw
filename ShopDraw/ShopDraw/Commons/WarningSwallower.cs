using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace ShopDraw.Commons
{
    public class WarningSwallower : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            // Lấy danh sách tất cả thông báo (Lỗi + Cảnh báo)
            IList<FailureMessageAccessor> failures = failuresAccessor.GetFailureMessages();

            if (failures.Count == 0) return FailureProcessingResult.Continue;

            //bool hasError = false;

            foreach (FailureMessageAccessor failure in failures)
            {
                // 1. Nếu là Cảnh báo (Warning) -> Xóa đi cho đỡ vướng mắt
                if (failure.GetSeverity() == FailureSeverity.Warning)
                {
                    failuresAccessor.DeleteWarning(failure);
                }
                // 2. Nếu là Lỗi (Error) -> thử resolve trước, rollback nếu không được
                else if (failure.GetSeverity() == FailureSeverity.Error)
                {
                    string failDesc = failure.GetDescriptionText();
                    List<ElementId> failingElements = failure.GetFailingElementIds().ToList();

                    if (failuresAccessor.IsFailureResolutionPermitted(failure))
                    {
                        failuresAccessor.ResolveFailure(failure);
                    }
                    else
                    {
                        return FailureProcessingResult.ProceedWithRollBack;
                    }
                }
            }

            return FailureProcessingResult.ProceedWithCommit;
        }
    }
}
