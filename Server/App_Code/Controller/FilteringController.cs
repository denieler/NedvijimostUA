﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for WebFilteringController
/// </summary>
public class FilteringController
{
    #region variables
    protected string _advertismentSectionCode;
    protected Log _log;

    protected string _webFilterProcessName;

    /// <summary>
    /// For long running operation execution
    /// </summary>
    private delegate void LongRun();
    #endregion variables

    public FilteringController(string advertismentSectionCode, string logFileName, string webFilterProcessName)
	{
        _advertismentSectionCode = advertismentSectionCode;
        _webFilterProcessName = webFilterProcessName;

        _log = new Log(logFileName);
	}

    public void StartFiltering()
    {
        var context = HttpContext.Current;
        var taskAppExecutingFlag = context.Application[_webFilterProcessName + "_TaskExecuted"];
        if (context != null
            && (taskAppExecutingFlag == null
               || 
               (taskAppExecutingFlag is bool && (bool)taskAppExecutingFlag == false))
            )
            try
            {
                HttpContext.Current.Application[_webFilterProcessName + "_TaskExecuted"] = true;

                _log.WriteLog("---------------------------------------------------------------------------" +
                             Environment.NewLine +
                             "Start " + _webFilterProcessName + " Web Filter processing.");
                var Long = new LongRun(Filter);
                var thread = new System.Threading.Thread(new System.Threading.ThreadStart(Long));

                var jobHost = new JobHost(_log);
                jobHost.DoWork(thread.Start);

            }
            catch (Exception exc)
            {
                _log.WriteLog("Filtering error! Error message: " + exc.Message);
            }
    }
    private void Filter()
    {
        try
        {
            var context = new Model.DataModel();

            var advertismentsWorkflow = new AdvertismentsWorkflow(context);
            var adversitmentsView = advertismentsWorkflow.LoadTodayAdversitments(AdvertismentsState.JustParsed, _advertismentSectionCode);

            if (adversitmentsView != null)
            {
                var advertsProcessing = new AdvertsProcessing(context);
                advertsProcessing.Log = _log;
                var adversitmentsWithoutSubpurchasers = advertsProcessing.FilterSubpurchasers(adversitmentsView.Advertisments, _advertismentSectionCode);
            }
        }
        catch (Exception e)
        {
            _log.WriteLog("Filtering inner error! Error message: " + e.Message + ". Trace:" + e.StackTrace);
        }
        finally
        {
            if (HttpContext.Current != null)
                HttpContext.Current.Application[_webFilterProcessName + "_TaskExecuted"] = false;
        }
    }
}