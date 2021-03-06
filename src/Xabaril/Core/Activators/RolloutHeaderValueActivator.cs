﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xabaril.Core.Activators
{
    public sealed class RolloutHeaderValueActivator
        : IFeatureActivator, IDiscoverableActivatorParameters
    {
        const string DEFAULT_HEADER_VALUE = "default header value";
        const int NUMBER_OF_PARTITIONS = 10;

        private ILogger<XabarilModule> _logger;
        private IRuntimeParameterAccessor _runtimeParameterAccessor;
        private IHttpContextAccessor _httpContextAccessor;

        List<ActivatorParameterDescriptor> _descriptors = new List<ActivatorParameterDescriptor>()
        {
            new ActivatorParameterDescriptor() {Name = "header-name", ClrType=typeof(String).Name , IsOptional = false,ActivatorName = typeof(RolloutHeaderValueActivator).Name},
            new ActivatorParameterDescriptor() {Name = "percentage", ClrType=typeof(Double).Name , IsOptional = false,ActivatorName = typeof(RolloutHeaderValueActivator).Name},
        };

        public IEnumerable<ActivatorParameterDescriptor> Descriptors
        {
            get
            {
                return _descriptors;
            }
        }

        public RolloutHeaderValueActivator(ILogger<XabarilModule> logger,
            IRuntimeParameterAccessor runtimeParameterAccessor,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _runtimeParameterAccessor = runtimeParameterAccessor ?? throw new ArgumentNullException(nameof(runtimeParameterAccessor));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public async Task<bool> IsActiveAsync(string featureName)
        {
            var active = false;

            var headerName = await _runtimeParameterAccessor
                .GetValueAsync<string>(featureName, _descriptors[0]);

            var percentage = await _runtimeParameterAccessor
                .GetValueAsync<double>(featureName, _descriptors[1]);

            if (headerName != null)
            {
                var headerValues = _httpContextAccessor.HttpContext.Request
                    .Headers[headerName];

                var value = headerValues[0] ?? DEFAULT_HEADER_VALUE;

                var assignedPartition = JenkinsPartitioner.ResolveToLogicalPartition(value, NUMBER_OF_PARTITIONS);

                return assignedPartition <= ((NUMBER_OF_PARTITIONS * percentage) / 100);
            }
            else
            {
                _logger.LogWarning($"The header name {_descriptors[0].Name} for feature {featureName} on RollupHeaderValueActivator is not configured correctly.");
            }

            return active;
        }
    }
}
