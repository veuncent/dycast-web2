﻿using DotSpatial.Projections;
using dycast_web.Data;
using dycast_web.Models.ViewModels.DycastViewModels;
using GeoJSON.Net.Feature;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dycast_web.Services
{
    public class RiskService : IRiskService
    {
        private readonly DycastDbContext _context;
        private readonly ProjectionInfo _projectionFrom;
        private readonly ProjectionInfo _projectionTo;

        public RiskService(DycastDbContext context)
        {
            _context = context;
            _projectionFrom = ProjectionInfo.FromEpsgCode(3857);
            _projectionTo = ProjectionInfo.FromEpsgCode(4326);
        }


        public async Task<FeatureCollection> GetRisk()
        {
            return await GetRisk(DateTime.MinValue, DateTime.MaxValue);
        }

        public async Task<FeatureCollection> GetRisk(DateTime? fromDate, DateTime? toDate)
        {
            var points = await _context.Risk.Select(r => new RiskPointViewModel
            {
                RiskDate = r.RiskDate,
                LatOriginal = r.Lat,
                LongOriginal = r.Long,
                PValue = r.CumulativeProbability,
                //ClosePairs = r.ClosePairs,
                //CloseSpace = r.CloseSpace,
                //CloseTime = r.CloseTime,
                //NumCases = r.NumBirds
            })
            .Where(r => r.RiskDate >= fromDate && r.RiskDate <= toDate)
            .ToListAsync();

            ReprojectPoints(points);

            var risk = new FeatureCollection();
            foreach (var point in points)
            {
                risk.Features.Add(point.AsGeoJsonFeature());
            }

            return risk;
        }


        private List<RiskPointViewModel> ReprojectPoints(List<RiskPointViewModel> riskPoints)
        {
            foreach (var riskpoint in riskPoints)
            {
                var pointsArray = new double[2];
                pointsArray[0] = riskpoint.LatOriginal;
                pointsArray[1] = riskpoint.LongOriginal;

                double[] z = { 0 };

                Reproject.ReprojectPoints(pointsArray, z, _projectionFrom, _projectionTo, 0, 1);

                riskpoint.Lat = pointsArray[0];
                riskpoint.Long = pointsArray[1];
            }
            return riskPoints;
        }

    }
}
