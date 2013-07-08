﻿using SiteMVC.Models.Engine.Advertisment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SiteMVC.Controllers.Controls
{
    public class AdvertismentsController : Controller
    {

        public ActionResult AdvertismentsList(SiteMVC.Models.UI.AdvertismentsRequest request)
        {
            AdvertismentsList advertisments;

            if (!string.IsNullOrEmpty(Request["page"]))
            {
                int currentPage;
                if (!int.TryParse(Request["page"], out currentPage) || currentPage < 1)
                    currentPage = 1;

                request.Offset = (currentPage - 1) * request.Limit;
            }

            if (request.Date == null)
            {
                SetTodayDate(request);

                advertisments = LoadAdversitments(request);
                if (!IsLoaded(advertisments))
                {
                    SetYesterdayDate(request);
                    advertisments = LoadAdversitments(request);
                }
            }
            else
                advertisments = LoadAdversitments(request);

            advertisments.Offset = request.Offset;
            advertisments.Limit = request.Limit;
            
            return PartialView("~/Views/Controls/AdvertismentsList.cshtml", advertisments);
        }

        #region Advertisments Loading
        private AdvertismentsList LoadAdversitments(SiteMVC.Models.UI.AdvertismentsRequest request)
        {
            IQueryable<Models.Advertisment> advertisments = LoadAdvertismentsByDate(request.DateFrom.Value, request.DateTo.Value);
            int advertismentsCount = 0;
            int advertismentsToShowCount = 0;
            if (advertisments != null)
            {
                advertisments = advertisments.Where(adv => adv.AdvertismentSection_Id == request.SectionId);

                if (request.SubSectionId != null)
                    advertisments = advertisments.Where(adv => adv.AdvertismentSubSection_Id == request.SubSectionId.Value);

                advertismentsCount = advertisments.Count();

                switch (request.State)
                {
                    case State.JustParsed:
                        advertisments = advertisments.Where(
                            adv => adv.subpurchaseAdvertisment && adv.SubPurchase_Id == null
                            );
                        break;
                    case State.NotSubpurchase:
                        advertisments = advertisments.Where(
                            adv => !adv.subpurchaseAdvertisment
                            );
                        break;
                    case State.Subpurchase:
                        advertisments = advertisments.Where(
                            adv => adv.subpurchaseAdvertisment && adv.SubPurchase_Id != null
                            );
                        break;
                    case State.SubpurchaseWithNotSubpurchase:
                        advertisments = advertisments.Where(
                            adv => !adv.subpurchaseAdvertisment || adv.SubPurchase_Id != null
                            );
                        break;
                }

                advertismentsToShowCount = advertisments.Count();

                //--- special filters
                if (request.OnlyWithPhotos)
                    advertisments = advertisments.Where(adv => adv.AdvertismentsPhotos.Any());

                advertisments = advertisments.Skip(request.Offset).Take(request.Limit);
            }

            DateTime lastTimeUpdated;
            if (advertisments != null && advertisments.Any())
                lastTimeUpdated = advertisments.Max(adv => adv.createDate);
            else lastTimeUpdated = DateTime.Now;

            return new AdvertismentsList(advertisments, advertismentsCount, advertismentsToShowCount, lastTimeUpdated);
        }

        private IQueryable<Models.Advertisment> LoadAdvertismentsByDate(DateTime dateTimeFrom, DateTime dateTimeTo)
        {
            var dataModel = new Models.DataModel();

            var specialFromDateTime = dateTimeFrom.Date.AddDays(-7);

            IQueryable<Models.Advertisment> searchResults =
                dataModel.Advertisments
                .Where(a =>
                    ((!a.isSpecial && a.createDate >= dateTimeFrom.Date) || (a.isSpecial && a.createDate >= specialFromDateTime.Date))
                    && a.createDate < dateTimeTo.Date
                    && !a.not_realestate
                    && !a.not_show_advertisment)
                .OrderByDescending(a => a.isSpecial)
                .OrderByDescending(a => a.createDate);
            return searchResults;
        }

        private bool IsLoaded(AdvertismentsList response)
        {
            return response == null
                   || (response != null
                       && response.Advertisments != null
                       && response.Advertisments.All(a => a.IsSpecial));
        }

        private void SetTodayDate(SiteMVC.Models.UI.AdvertismentsRequest request)
        {
            request.DateFrom = SystemUtils.Utils.Date.GetUkranianDateTimeNow().Date;
            request.DateTo = SystemUtils.Utils.Date.GetUkranianDateTimeNow().Date.AddDays(1);
        }

        private void SetYesterdayDate(SiteMVC.Models.UI.AdvertismentsRequest request)
        {
            request.DateFrom = SystemUtils.Utils.Date.GetUkranianDateTimeNow().AddDays(-1).Date;
            request.DateTo = SystemUtils.Utils.Date.GetUkranianDateTimeNow().Date;
        }
        #endregion Advertisments Loading

    }
}