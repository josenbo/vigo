﻿using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Serilog;
using Serilog.Events;

namespace vigobase;

[PublicAPI]
public class FaultRegistry
{
    public IEnumerable<FaultIncident> Incidents => _incidents;
    
    public string Fatal(
        string faultKey,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var incidentId = GetNextIncidentId(faultKey);
        var incident = new FaultIncident(incidentId, message, faultKey, LogEventLevel.Fatal, DateTime.Now, memberName, sourceFilePath, sourceLineNumber);
        _incidents.Add(incident);
        Log.Fatal("Registered fault incident {TheIncident}", incident);
        return incidentId;
    }

    private readonly List<FaultIncident> _incidents = [];
    
    private static string GetNextIncidentId(string faultKey)
    {
        var incidentId = _nextIncidentId++; 
        // ReSharper disable once StringLiteralTypo
        return $"{faultKey}_{incidentId}";
    }

    private static int _nextIncidentId = Random.Shared.Next(1000, 9000);
}