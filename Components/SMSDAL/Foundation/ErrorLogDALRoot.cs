using Microsoft.Data.SqlClient;
using SMSDomainModels.Foundation;
using System.Data;

namespace SMSDAL.Foundation
{
    public class ErrorLogDALRoot
    {
        private readonly string _connectionStr;

        public ErrorLogDALRoot(string connectionStr)
        {
            _connectionStr = connectionStr;
        }

        public virtual async Task<bool> SaveErrorObjectInDb(ErrorLogRoot errorLog)
        {
            using SqlConnection conn = new SqlConnection(_connectionStr);
            string sqlCmdTxt = "INSERT INTO [ErrorLogRoots] ([loginUserId],[UserRoleType],[CompanyCode],[CreatedByApp],[CreatedOnUTC],[LogMessage],[LogStackTrace],[LogExceptionData],[innerException],[TracingId],[Caller],[RequestObject],[ResponseObject],[AdditionalInfo]) VALUES (@LoginUserId,@UserRoleType,@CompanyCode,@CreatedByApp,@CreatedOnUTC,@LogMessage,@LogStackTrace,@LogExceptionData,@InnerException,@TracingId,@Caller,@RequestObject,@ResponseObject,@AdditionalInfo)";
            SqlCommand insertlogCommand = conn.CreateCommand();
            insertlogCommand.CommandText = sqlCmdTxt;
            insertlogCommand.Parameters.Add(new SqlParameter
            {
                DbType = DbType.String,
                ParameterName = "LoginUserId",
                Value = errorLog?.LoginUserId ?? ""
            });
            insertlogCommand.Parameters.Add(new SqlParameter
            {
                DbType = DbType.String,
                ParameterName = "UserRoleType",
                Value = errorLog?.UserRoleType ?? ""
            });
            insertlogCommand.Parameters.Add(new SqlParameter
            {
                DbType = DbType.String,
                ParameterName = "CompanyCode",
                Value = errorLog?.CompanyCode ?? ""
            });
            insertlogCommand.Parameters.Add(new SqlParameter
            {
                DbType = DbType.String,
                ParameterName = "CreatedByApp",
                Value = errorLog?.CreatedByApp ?? ""
            });
            insertlogCommand.Parameters.Add(new SqlParameter
            {
                DbType = DbType.DateTime,
                ParameterName = "CreatedOnUTC",
                Value = errorLog?.CreatedOnUTC
            });
            insertlogCommand.Parameters.Add(new SqlParameter
            {
                DbType = DbType.String,
                ParameterName = "LogMessage",
                Value = errorLog?.LogMessage ?? ""
            });
            insertlogCommand.Parameters.Add(new SqlParameter
            {
                DbType = DbType.String,
                ParameterName = "LogStackTrace",
                Value = errorLog?.LogStackTrace ?? ""
            });
            insertlogCommand.Parameters.Add(new SqlParameter
            {
                DbType = DbType.String,
                ParameterName = "LogExceptionData",
                Value = errorLog?.LogExceptionData ?? ""
            });
            insertlogCommand.Parameters.Add(new SqlParameter
            {
                DbType = DbType.String,
                ParameterName = "InnerException",
                Value = errorLog?.InnerException ?? ""
            });
            insertlogCommand.Parameters.Add(new SqlParameter
            {
                DbType = DbType.String,
                ParameterName = "TracingId",
                Value = errorLog?.TracingId ?? ""
            });
            insertlogCommand.Parameters.Add(new SqlParameter
            {
                DbType = DbType.String,
                ParameterName = "Caller",
                Value = errorLog?.Caller ?? ""
            });
            insertlogCommand.Parameters.Add(new SqlParameter
            {
                DbType = DbType.String,
                ParameterName = "RequestObject",
                Value = errorLog?.RequestObject ?? ""
            });
            insertlogCommand.Parameters.Add(new SqlParameter
            {
                DbType = DbType.String,
                ParameterName = "ResponseObject",
                Value = errorLog?.ResponseObject ?? ""
            });
            insertlogCommand.Parameters.Add(new SqlParameter
            {
                DbType = DbType.String,
                ParameterName = "AdditionalInfo",
                Value = errorLog?.AdditionalInfo ?? ""
            });
            if (insertlogCommand.Connection.State != ConnectionState.Open)
            {
                await insertlogCommand.Connection.OpenAsync();
            }

            return await insertlogCommand.ExecuteNonQueryAsync() >= 1;
        }
    }
}
