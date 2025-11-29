using System;
using System.Collections.Generic;

namespace ProcessManager.Domain.Exceptions
{
    public class ReportingException : Exception
    {
        public ReportingException(IEnumerable<string> dboEntities, DateTime? fromDate, DateTime? toDatetime)
            : base($"Reporting for: {string.Join(", ", dboEntities)} with fromDate: {fromDate} and toDatetime: {toDatetime}") { }
    }
}
