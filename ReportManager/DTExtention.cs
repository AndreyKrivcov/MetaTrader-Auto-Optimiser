using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportManager
{
    /// <summary>
    /// расширение для преобразования Unix дат к DateTime
    /// </summary>
    public static class DTExtention
    {
        /// <summary>
        /// начало эпохи юикс
        /// </summary>
        public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        /// <summary>
        /// юникс дата к DateTime
        /// </summary>
        /// <param name="Time">юникс время</param>
        /// <returns>конвертированное время</returns>
        public static DateTime UnixDTToDT(this ulong Time)
        {
            return UnixEpoch.AddSeconds(Time);
        }

        /// <summary>
        /// DateTime к юникс дате
        /// </summary>
        /// <param name="DT">переданное время</param>
        /// <returns>конвертированное время</returns>
        public static ulong DTToUnixDT(this DateTime DT)
        {
            return (ulong)(DT - UnixEpoch).TotalSeconds;
        }
    }
}
