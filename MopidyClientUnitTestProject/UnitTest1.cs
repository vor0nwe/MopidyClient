using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MopidyClientUnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestDateTimeToIntAndBack()
        {
            var OriginalDate = DateTime.Now;
            var IntDate = MopidyTray.Models.BaseModel.Utils.DateTimeToInt(OriginalDate);
            var Date = MopidyTray.Models.BaseModel.Utils.IntToDateTime(IntDate);
            if (Date.ToLocalTime() != OriginalDate.ToLocalTime())
                throw new ArgumentOutOfRangeException("Date", Date, OriginalDate.ToString());

        }
    }
}
