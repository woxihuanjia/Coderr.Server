﻿using System;
using System.Threading.Tasks;
using Coderr.Server.Abstractions.Boot;
using Coderr.Server.Abstractions.Config;
using Coderr.Server.App.Core.Reports.Config;
using Griffin.ApplicationServices;
using Griffin.Data;
using log4net;

namespace Coderr.Server.App.Core.Reports.Jobs
{
    /// <summary>
    ///     Will delete all reports which is older than the configured (<see cref="ReportConfig.RetentionDays" />) retention
    ///     period.
    /// </summary>
    [ContainerService(RegisterAsSelf = true)]
    public class DeleteOldReports : IBackgroundJobAsync
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(DeleteOldReports));
        private readonly IAdoNetUnitOfWork _unitOfWork;
        private readonly IConfiguration<ReportConfig> _reportConfig;

        /// <summary>
        ///     Creates a new instance of <see cref="DeleteOldReports" />.
        /// </summary>
        /// <param name="unitOfWork">Used for SQL queries</param>
        /// <param name="reportConfig"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public DeleteOldReports(IAdoNetUnitOfWork unitOfWork, IConfiguration<ReportConfig> reportConfig)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _reportConfig = reportConfig;
        }

        /// <summary>
        ///     Number of reports which can be stored per incident.
        /// </summary>
        public int MaxReportsPerIncident
        {
            get { return _reportConfig.Value.MaxReportsPerIncident; }
        }

        /// <summary>
        ///     Number of days to keep old reports.
        /// </summary>
        public int RetentionDays
        {
            get
            {
                return _reportConfig.Value.RetentionDays;
            }
        }

        /// <inheritdoc />
        public async Task ExecuteAsync()
        {
            using (var cmd = _unitOfWork.CreateDbCommand())
            {
                var sql = @"DELETE TOP(1000) FROM ErrorReports WHERE CreatedAtUtc < @date";
                cmd.CommandText = sql;
                cmd.AddParameter("date", DateTime.UtcNow.AddDays(-RetentionDays));
                cmd.CommandTimeout = 90;
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows > 0)
                {
                    _logger.Debug("Deleted the oldest " + rows + " reports.");
                }
            }
        }
        
    }
}