﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using LibApAgentData.Models;
using LibParameters;
using LibToolActions.BackgroundTasks;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace ReServer;

public sealed class JobStarter
{
    private readonly string _apAgentParametersFileName;

    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Dictionary<string, DateTime> _nextRunDatesByScheduleNames = [];
    private readonly ParametersLoader<ApAgentParameters> _parLoader;
    private readonly IProcesses _processes;
    private DateTime _nextJobDateTime;


    // ReSharper disable once ConvertToPrimaryConstructor
    public JobStarter(ILogger logger, IHttpClientFactory httpClientFactory, IProcesses processes,
        string apAgentParametersFileName, string encKey)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _processes = processes;
        _apAgentParametersFileName = apAgentParametersFileName;
        _parLoader = new ParametersLoader<ApAgentParameters>(encKey);
    }

    public void Run()
    {
        _logger.LogInformation("JobStarter.Run started");

        //მოვძებნოთ თუ არსებობს ისეთი დაგეგმვა,
        //რომელიც ითვალისწინებს სამუშაოს გაშვებას პროგრამის გაშვებისთანავე
        //თუ ასეთი დაგეგმვა მოიძებნა გაეშვას შესაბამისი სამუშაო ცალკე ნაკადში
        FindRunStartUpJobs(false);
    }

    private void FindRunStartUpJobs(bool byTime)
    {
        _parLoader.TryLoadParameters(_apAgentParametersFileName);
        var parameters = (ApAgentParameters?)_parLoader.Par;
        if (parameters == null)
            return;

        var jobSchedulesDict =
            parameters.GetStartUpJobSchedules(byTime, _nextRunDatesByScheduleNames);

        //LocalPathCounter procLogFilesPathCounter =
        //    LocalPathCounterFabric.CreateProcLogFilesPathCounter(parameters, _apAgentParametersFileName);

        var procLogFilesFolder = parameters.CountLocalPath(parameters.ProcLogFilesFolder,
            _apAgentParametersFileName, "ProcLogFiles");

        if (string.IsNullOrWhiteSpace(procLogFilesFolder))
        {
            _logger.LogInformation("procLogFilesFolder does not specified");
            return;
        }

        if (!StShared.CreateFolder(procLogFilesFolder, false))
        {
            _logger.LogInformation("{procLogFilesFolder} does not exists and cannot be created", procLogFilesFolder);
            return;
        }

        //string procLogFilesFolder = procLogFilesPathCounter.Count(parameters.ProcLogFilesFolder);


        foreach (var kvp in jobSchedulesDict)
        {
            //if (parameters.JobSchedules == null || !parameters.JobSchedules.ContainsKey(scheduleName))
            //{
            //  _logger.LogError($"Schedules with name {jobSchedule.JobStepNames} not found");
            //}

            //ეს მინიჭება საჭიროა იმისათვის რომ მერე მოხდეს ახალი დროის დაანგარიშება
            _nextJobDateTime = DateTime.MaxValue;

            parameters.RunAllSteps(_logger, _httpClientFactory, false, kvp.Key, _processes, procLogFilesFolder);
        }

        if (_nextJobDateTime == DateTime.MaxValue)
            CalculateNextStartTime(parameters);
    }

    private void CalculateNextStartTime(ApAgentParameters parameters)
    {
        //ყველა არსებული დაგეგმვისათვის მოხდეს იმის დაანგარიშება, თუ როდის უწევს შემდეგი გაშვება
        //მათ შორის გამოვითვალოთ მინიმალური და დავიმახსოვროთ ცვლადში,
        //რომელიც გვაჩვენებს შემდეგი დათვლის გაშვების დროს
        //ეს ცვლადია nextJobDateTime
        var minNexStartTime = DateTime.MaxValue;

        //ამ პროცესის ჩატარების დროს აგრეთვე უნდა შევინახოთ ყველა დაგეგმვის შესაბამისი შემდეგი გაშვების დრო
        //0;AtStart;1;Once;2;Daily;3;weekly;4;monthly;5;WhenCPUIdle
        //IEnumerable<DsJobs.JobSchedulesRow> startUpJobSchedules = ProcData.Instance.GetNotStartUpJobSchedules();
        var jobSchedulesDict = parameters.GetNotStartUpJobSchedules();

        foreach (var kvp in jobSchedulesDict)
        {
            var nextDateTime = RecountNextStartTime(kvp);

            _nextRunDatesByScheduleNames[kvp.Key] = nextDateTime;

            _logger.LogInformation("NextStartTime for Job Schedule={Key} - {nextDateTime}", kvp.Key, nextDateTime);
            if (nextDateTime < minNexStartTime)
                minNexStartTime = nextDateTime;
        }

        _nextJobDateTime = minNexStartTime;
        _logger.LogInformation("Minimum NextStartTime for all jobs is {minNexStartTime}", minNexStartTime);
    }


    private static DateTime RecountNextStartTime(KeyValuePair<string, JobSchedule> kvp)
    {
        var jobSchedule = kvp.Value;

        //TimeSpan toRet = TimeSpan.MaxValue;
        //0;AtStart;1;Once;2;Daily;3;weekly;4;monthly;5;WhenCPUIdle

        var firstDateTime = jobSchedule.DurationStartDate.Add(new TimeSpan(
            jobSchedule.ActiveStartDayTime.Hours,
            jobSchedule.ActiveStartDayTime.Minutes, jobSchedule.ActiveStartDayTime.Seconds));
        var nowDateTime = DateTime.Now;


        switch (jobSchedule.ScheduleType)
        {
            case EScheduleType.Once:
                return firstDateTime < nowDateTime ? DateTime.MaxValue : firstDateTime;
            case EScheduleType.Daily:

                var toDayStart = DateTime.Today.Add(jobSchedule.ActiveStartDayTime);
                var toDayEnd = DateTime.Today.Add(jobSchedule.ActiveEndDayTime);


                var daysToAdd = (int)(nowDateTime.Date - firstDateTime.Date).TotalDays / jobSchedule.FreqInterval *
                                jobSchedule.FreqInterval;
                var candidateStartDateTime = firstDateTime.AddDays(daysToAdd);
                var nextCandidateStartDateTime = candidateStartDateTime;
                if (candidateStartDateTime < nowDateTime)
                    nextCandidateStartDateTime = candidateStartDateTime.AddDays(jobSchedule.FreqInterval);

                if (jobSchedule.DailyFrequencyType == EDailyFrequency.OccursOnce ||
                    candidateStartDateTime.Date != nowDateTime.Date || toDayEnd < nowDateTime)
                    return nextCandidateStartDateTime.Date <= jobSchedule.DurationEndDate.Date
                        ? nextCandidateStartDateTime
                        : DateTime.MaxValue;


                var atStartMinutes = toDayStart.Hour * 60 + toDayStart.Minute;
                var minuteInterval = jobSchedule.FreqSubDayInterval;
                if (jobSchedule.FreqSubDayType == EEveryMeasure.Hour)
                    minuteInterval = jobSchedule.FreqSubDayInterval * 60;

                //toRet = now.Date.AddHours(now.Hour).AddMinutes(now.Minute);
                var curMinutes = nowDateTime.Hour * 60 + nowDateTime.Minute;
                var minutesToAdd = (curMinutes - atStartMinutes) / minuteInterval * minuteInterval;
                var candidateDateTime = toDayStart.AddMinutes(minutesToAdd);
                var nextCandidateDateTime = candidateStartDateTime;
                if (candidateDateTime < nowDateTime)
                    nextCandidateDateTime = candidateDateTime.AddMinutes(minuteInterval);

                if (nextCandidateDateTime < toDayEnd &&
                    nextCandidateDateTime.Date <= jobSchedule.DurationEndDate.Date)
                    return nextCandidateDateTime;

                return nextCandidateStartDateTime.Date <= jobSchedule.DurationEndDate.Date
                    ? nextCandidateStartDateTime
                    : DateTime.MaxValue;
        }

        return DateTime.MaxValue;
    }


    public void DoTimerEventAnswer()
    {
        //დავადგინოთ მოვიდა თუ არა შემდეგი გადაანგარიშების დრო
        //ამისათვის ჩატვირთულ ინფორმაციაში უნდა მოვიძიოთ
        //რომელ სამუშაოს აქვს ის დრო რომელიც ეხლაა

        //ითვლება რომ ახალ სამუშაოს არ აინტერესებს ძველი დამთავრდა თუ არა
        //(თუ საჭირო გახდა ამის მართვა მერეც შეგვიძლია დავამატოთ)

        if (_nextJobDateTime <= DateTime.Now)
            FindRunStartUpJobs(true);
        else
            _logger.LogInformation("next Job Date Time is: {_nextJobDateTime}, ", _nextJobDateTime);
    }
}