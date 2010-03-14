﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DDay.iCal
{
    public class RecurringComponentPeriodEvaluator :
        PeriodEvaluator
    {
        #region Private Fields

        private IRecurringComponent m_Component;

        #endregion

        #region Protected Properties

        protected IRecurringComponent Component
        {
            get { return m_Component; }
            set { m_Component = value; }
        }

        #endregion

        #region Constructors

        public RecurringComponentPeriodEvaluator(IRecurringComponent comp)
        {
            Component = comp;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Evaulates the RRule component, and adds each specified Period
        /// to the <see cref="Periods"/> collection.
        /// </summary>
        /// <param name="FromDate">The beginning date of the range to evaluate.</param>
        /// <param name="ToDate">The end date of the range to evaluate.</param>
        virtual protected void EvaluateRRule(iCalDateTime from, iCalDateTime to)
        {
            // Handle RRULEs
            if (Component.RecurrenceRules != null)
            {
                foreach (IRecurrencePattern rrule in Component.RecurrenceRules)
                {
                    IPeriodEvaluator evaluator = rrule.GetService(typeof(IPeriodEvaluator)) as IPeriodEvaluator;
                    if (evaluator != null)
                    {
                        //// Get a list of static occurrences
                        //// This is important to correctly calculate
                        //// recurrences with COUNT.
                        // FIXME: static occurrences can be added elsewhere.
                        //evaluator.StaticOccurrences.Clear();
                        //foreach (Period p in Periods)
                        //    evaluator.StaticOccurrences.Add(p.StartTime);

                        ////
                        //// Determine the last allowed date in this recurrence
                        ////
                        // FIXME: can this be removed?
                        //if (rrule.Until.IsAssigned && (!m_Until.IsAssigned || m_Until < rrule.Until))
                        //    m_Until = rrule.Until;

                        IList<Period> periods = evaluator.Evaluate(Component.Start, from, to);
                        foreach (Period p in periods)
                        {
                            if (!Periods.Contains(p))
                                this.Periods.Add(p);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Evalates the RDate component, and adds each specified DateTime or
        /// Period to the <see cref="Periods"/> collection.
        /// </summary>
        /// <param name="FromDate">The beginning date of the range to evaluate.</param>
        /// <param name="ToDate">The end date of the range to evaluate.</param>
        virtual protected void EvaluateRDate(iCalDateTime fromTime, iCalDateTime toTime)
        {
            // Handle RDATEs
            if (Component.RecurrenceDates != null)
            {
                foreach (IRecurrenceDate rdate in Component.RecurrenceDates)
                {
                    IPeriodEvaluator evaluator = rdate.GetService(typeof(IPeriodEvaluator)) as IPeriodEvaluator;
                    if (evaluator != null)
                    {
                        IList<Period> periods = evaluator.Evaluate(Component.Start, fromTime, toTime);
                        foreach (Period p in periods)
                        {
                            if (!Periods.Contains(p))
                            {
                                Periods.Add(p);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Evaulates the ExRule component, and excludes each specified DateTime
        /// from the <see cref="Periods"/> collection.
        /// </summary>
        /// <param name="FromDate">The beginning date of the range to evaluate.</param>
        /// <param name="ToDate">The end date of the range to evaluate.</param>
        virtual protected void EvaluateExRule(iCalDateTime from, iCalDateTime to)
        {
            // Handle EXRULEs
            if (Component.ExceptionRules != null)
            {
                foreach (IRecurrencePattern exrule in Component.ExceptionRules)
                {
                    IPeriodEvaluator evaluator = exrule.GetService(typeof(IPeriodEvaluator)) as IPeriodEvaluator;
                    if (evaluator != null)
                    {
                        IList<Period> periods = evaluator.Evaluate(Component.Start, from, to);
                        for (int i = 0; i < periods.Count; i++)
                        {
                            Period p = periods[i];
                            if (this.Periods.Contains(p))
                                this.Periods.Remove(p);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Evalates the ExDate component, and excludes each specified DateTime or
        /// Period from the <see cref="Periods"/> collection.
        /// </summary>
        /// <param name="FromDate">The beginning date of the range to evaluate.</param>
        /// <param name="ToDate">The end date of the range to evaluate.</param>
        virtual protected void EvaluateExDate(iCalDateTime FromDate, iCalDateTime ToDate)
        {
            // Handle EXDATEs
            if (Component.ExceptionDates != null)
            {
                foreach (IRecurrenceDate exdate in Component.ExceptionDates)
                {
                    IPeriodEvaluator evaluator = exdate.GetService(typeof(IPeriodEvaluator)) as IPeriodEvaluator;
                    if (evaluator != null)
                    {
                        IList<Period> periods = evaluator.Evaluate(Component.Start, FromDate, ToDate);
                        for (int i = 0; i < periods.Count; i++)
                        {
                            Period p = periods[i];

                            // If no time was provided for the ExDate, then it excludes the entire day
                            if (!p.StartTime.HasTime || (p.EndTime != null && !p.EndTime.HasTime))
                                p.MatchesDateOnly = true;

                            while (Periods.Contains(p))
                                Periods.Remove(p);
                        }
                    }
                }
            }
        }

        #endregion

        #region Overrides

        public override IList<Period> Evaluate(iCalDateTime startTime, iCalDateTime fromTime, iCalDateTime toTime)
        {
            // Evaluate extra time periods, without re-evaluating ones that were already evaluated
            if ((!EvaluationStartBounds.IsAssigned && !EvaluationEndBounds.IsAssigned) ||
                (toTime == EvaluationStartBounds) ||
                (fromTime == EvaluationEndBounds))
            {
                EvaluateRRule(fromTime, toTime);
                EvaluateRDate(fromTime, toTime);
                EvaluateExRule(fromTime, toTime);
                EvaluateExDate(fromTime, toTime);
                if (!EvaluationStartBounds.IsAssigned || EvaluationStartBounds > fromTime)
                    EvaluationStartBounds = fromTime;
                if (!EvaluationEndBounds.IsAssigned || EvaluationEndBounds < toTime)
                    EvaluationEndBounds = toTime;
            }

            if (EvaluationStartBounds.IsAssigned && fromTime < EvaluationStartBounds)
                Evaluate(startTime, fromTime, EvaluationStartBounds);
            if (EvaluationEndBounds.IsAssigned && toTime > EvaluationEndBounds)
                Evaluate(startTime, EvaluationEndBounds, toTime);

            // Sort the list
            m_Periods.Sort();

            for (int i = 0; i < Periods.Count; i++)
            {
                Period p = Periods[i];

                // Ensure the Kind of time is consistent with DTStart
                iCalDateTime pStart = p.StartTime;
                pStart.IsUniversalTime = Component.Start.IsUniversalTime;
                p.StartTime = pStart;

                Periods[i] = p;
            }

            return Periods;
        }

        #endregion
    }
}
