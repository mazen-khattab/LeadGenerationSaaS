using SaaS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Application.Common.Interfaces
{
    /// <summary>
    /// Encapsulates how to resolve the base URL and apply authentication for one specific
    /// external system. Adding a new external system means adding a new implementation of
    /// this interface — NetworkClient itself never needs to change (Open/Closed Principle).
    /// </summary>
    public interface IExternalSystemRequestStrategy
    {
        ExternalSystem System { get; }
        string ResolveBaseUrl(string endpoint);
        void ApplyAuthentication(HttpRequestMessage request);
    }
}
