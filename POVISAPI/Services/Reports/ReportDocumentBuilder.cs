using POVISAPI.Models.DTO;

namespace POVISAPI.Services.Reports
{
    /// <summary>Lays each report out like its sheet in the POVIS.xlsx sample.</summary>
    public static class ReportDocumentBuilder
    {
        private static string Period(DateOnly from, DateOnly to) =>
            from == to ? from.ToString("MMMM d, yyyy") : $"{from:MMMM d, yyyy} - {to:MMMM d, yyyy}";

        private static ReportColumn Col(string header, double width, ColumnKind kind = ColumnKind.Integer) =>
            new() { Header = header, Width = width, Kind = kind };

        private static int? NullIfZero(int n) => n == 0 ? null : n;

        // ── OVERALL ─────────────────────────────────────────────────────────
        public static ReportDocument Overall(OrdinanceSummaryReportDTO r)
        {
            var table = new ReportTable
            {
                Columns =
                {
                    Col("POD-OVR IMPLEMENTED CITY ORDINANCES", 9, ColumnKind.Text),
                    Col("FEMALE", 1.2), Col("MALE", 1.2), Col("TOTAL VIOLATOR", 1.6),
                    Col("SEMINAR", 1.3), Col("COUNSELLING", 1.6), Col("COMMUNITY SERVICE", 1.8),
                    Col("DID NOT COMPLY", 1.5), Col("TOTAL PAID", 1.2),
                    Col("AMOUNT OF FINES", 1.9, ColumnKind.Money),
                    Col("TOTAL AMOUNT PAID", 2.2, ColumnKind.Money),
                    Col("TOTAL AMOUNT UNPAID", 2.2, ColumnKind.Money),
                    Col("REMARKS", 2, ColumnKind.Text)
                },
                TotalRow = new object?[]
                {
                    "TOTAL", r.TotalFemale, r.TotalMale, r.TotalViolators, r.TotalSeminar, r.TotalCounseling,
                    r.TotalCommunityService, r.TotalDidNotComply, r.TotalPaid, null, r.TotalAmountPaid, r.TotalAmountUnpaid, null
                }
            };
            foreach (var x in r.Rows)
                table.Rows.Add(new object?[]
                {
                    x.Label, x.FemaleCount, x.MaleCount, x.TotalViolators, x.SeminarCount, x.CounselingCount,
                    x.CommunityServiceCount, x.DidNotComplyCount, x.PaidCount, x.FineAmount, x.TotalAmountPaid, x.TotalAmountUnpaid, null
                });

            return new ReportDocument
            {
                FileBaseName = $"povis-overall-{r.From:yyyyMMdd}-{r.To:yyyyMMdd}",
                Sheets =
                {
                    new ReportSheet
                    {
                        Name = "OVERALL",
                        
                        TitleLines = { "POD OVR:", Period(r.From, r.To) },
                        Tables = { table },
                        Notes =
                        {
                            $"TOTAL VIOLATORS: {r.TotalViolators:N0}",
                            "NOTE:",
                            "Total amount unpaid = fine amount x violators who have not paid (excluding dismissed and closed cases). Warning tickets are not counted."
                        }
                    }
                }
            };
        }

        // ── % PER BRGY & PER ORDINANCES ──────────────────────────────────────
        public static ReportDocument PerBarangay(PerBarangayReportDTO r)
        {
            var table = new ReportTable
            {
                Columns =
                {
                    Col("DISTRICT", 1.3, ColumnKind.Center), Col("NO.", 0.9), Col("BARANGAY", 3.4, ColumnKind.Text)
                }
            };
            foreach (var o in r.OrdinanceColumns) table.Columns.Add(Col(o.ToUpperInvariant(), 1.5));
            table.Columns.Add(Col("TOTAL", 1.5));
            table.Columns.Add(Col("%", 1.5, ColumnKind.Percent));

            int? lastDistrict = -1;
            var n = 0;
            foreach (var row in r.Rows)
            {
                n++;
                var cells = new List<object?>
                {
                    row.District != lastDistrict ? row.District?.ToString() ?? "-" : null,
                    n,
                    row.BarangayName
                };
                lastDistrict = row.District;
                foreach (var o in r.OrdinanceColumns)
                    cells.Add(row.CountsByOrdinance.TryGetValue(o, out var c) ? NullIfZero(c) : null);
                cells.Add(row.Total);
                cells.Add(row.Share);
                table.Rows.Add(cells.ToArray());
            }

            var totals = new List<object?> { "TOTAL", null, null };
            var shares = new List<object?> { null, null, null };
            foreach (var o in r.OrdinanceColumns)
            {
                var t = r.TotalsByOrdinance.GetValueOrDefault(o);
                totals.Add(t);
                shares.Add(r.GrandTotal == 0 ? 0d : (double)t / r.GrandTotal);
            }
            totals.Add(r.GrandTotal); totals.Add(r.GrandTotal == 0 ? 0d : 1d);
            shares.Add(null); shares.Add(null);

            var doc = new ReportDocument
            {
                FileBaseName = $"povis-per-barangay-{r.From:yyyyMMdd}-{r.To:yyyyMMdd}",
                Sheets =
                {
                    new ReportSheet
                    {
                        Name = "% PER BRGY & PER ORDINANCES",
                        Wide = true,
                        TitleLines = { "POD OVR PER BARANGAY", Period(r.From, r.To) },
                        Tables = { new ReportTable { Columns = table.Columns, Rows = table.Rows, TotalRow = totals.ToArray(), SecondTotalRow = shares.ToArray() } }
                    }
                }
            };
            return doc;
        }

        // ── TALAKAYAN SEMINAR PER BRGY ───────────────────────────────────────
        public static ReportDocument SeminarPerBarangay(SeminarBarangayReportDTO r)
        {
            var doc = new ReportDocument
            {
                FileBaseName = $"povis-seminar-per-barangay-{r.From:yyyyMMdd}-{r.To:yyyyMMdd}"
            };

            foreach (var b in r.Barangays)
            {
                var name = b.BarangayName.ToUpperInvariant();
                var by = b.Divisions.Count == 0 ? "" : $", apprehended by {JoinAnd(b.Divisions)}";
                var summary = $"{b.Recorded} Recorded Violations{by}. {b.Complied} complied, {b.NotComplied} not complied and {b.CaseFiled} Case Filed"
                            + (b.Dismissed > 0 ? $", {b.Dismissed} dismissed." : ".");
                var withApprehension = b.Enforcers.Count;
                var enforcerLine = $"{b.Enforcers.Count} Deputized Enforcer(s) of Brgy {b.BarangayName} with {b.Rows.Count} total apprehension(s)";

                var violations = new ReportTable
                {
                    Columns =
                    {
                        Col("NO.", 0.8), Col("DATE", 1.8, ColumnKind.Date), Col("APPREHENDING OFFICER", 2.6, ColumnKind.Center),
                        Col("GENDER", 1.2, ColumnKind.Center), Col("AGE", 0.9), Col("OVR NUMBER", 1.5, ColumnKind.Center),
                        Col("BARANGAY Place of Occurrence", 2.6, ColumnKind.Text), Col("CITY ORDINANCE VIOLATED", 2.6, ColumnKind.Text),
                        Col("CITY ORDINANCE NAME", 3.6, ColumnKind.Text), Col("REMARKS", 4, ColumnKind.Text)
                    }
                };
                var n = 0;
                foreach (var v in b.Rows)
                {
                    n++;
                    violations.Rows.Add(new object?[]
                    {
                        n, v.Date, v.Division, v.Gender, v.Age, v.OvrNumber, v.Place.ToUpperInvariant(), v.OrdinanceCode, v.OrdinanceName, v.Remarks
                    });
                }

                var enforcers = new ReportTable
                {
                    Heading = "Deputized Enforcers",
                    Columns = { Col("NO.", 0.8), Col("ENFORCER NAME", 4, ColumnKind.Text), Col("DEPUTATION NUMBER", 2.4, ColumnKind.Center), Col("APPREHENSIONS", 1.8) },
                    EmptyMessage = "No apprehending officers recorded."
                };
                n = 0;
                foreach (var e in b.Enforcers)
                {
                    n++;
                    enforcers.Rows.Add(new object?[] { n, e.Name.ToUpperInvariant(), e.DeputationNumber, e.Apprehensions });
                }

                doc.Sheets.Add(new ReportSheet
                {
                    Name = b.BarangayName,
                    TitleLines = { "TALAKAYAN SEMINAR", $"BARANGAY {name}" },
                    InfoLines = { $"BARANGAY {name} - {Period(r.From, r.To)}", summary, enforcerLine },
                    Tables = { violations, enforcers }
                });
            }

            if (doc.Sheets.Count == 0)
                doc.Sheets.Add(new ReportSheet
                {
                    Name = "TALAKAYAN SEMINAR",
                    TitleLines = { "TALAKAYAN SEMINAR PER BARANGAY", Period(r.From, r.To) },
                    Tables = { new ReportTable { Columns = { Col("NO.", 1), Col("DATE", 2, ColumnKind.Date), Col("OVR NUMBER", 2, ColumnKind.Center), Col("REMARKS", 5, ColumnKind.Text) } } }
                });

            return doc;
        }

        // ── POD / PNP / BSF INCENTIVES ───────────────────────────────────────
        public static ReportDocument Incentives(IncentiveReportDTO r)
        {
            var doc = new ReportDocument { FileBaseName = $"povis-incentives-{r.From:yyyyMMdd}-{r.To:yyyyMMdd}" };
            var rate = $"{r.Rate:P0}";

            foreach (var d in r.Divisions)
            {
                var payments = new ReportTable
                {
                    Columns =
                    {
                        Col("DATE OF PAYMENT", 2, ColumnKind.Date), Col("DIVISION", 1.6, ColumnKind.Center),
                        Col("DEPUTIZED", 3.4, ColumnKind.Text), Col("AMOUNT", 1.8, ColumnKind.Money), Col(rate, 1.6, ColumnKind.Money)
                    },
                    TotalRow = new object?[] { "TOTAL", null, null, d.TotalAmount, d.TotalIncentive }
                };
                foreach (var p in d.Payments)
                    payments.Rows.Add(new object?[] { p.DateOfPayment, p.Division, p.Deputized, p.Amount, p.Incentive });

                var totals = new ReportTable
                {
                    Heading = "TOTAL INCENTIVE PER DEPUTIZED",
                    Columns = { Col("DEPUTIZED", 4, ColumnKind.Text), Col("INCENTIVE", 2, ColumnKind.Money) },
                    TotalRow = new object?[] { "TOTAL", d.TotalIncentive }
                };
                foreach (var e in d.EnforcerTotals)
                    totals.Rows.Add(new object?[] { e.Deputized, e.TotalIncentive });

                doc.Sheets.Add(new ReportSheet
                {
                    Name = $"{d.Division} INCENTIVES",
                    TitleLines = { $"{d.Division} INCENTIVES", Period(r.From, r.To) },
                    Tables = { payments, totals },
                    Notes = { $"Incentive = {rate} of each fine paid, credited to the apprehending deputized enforcer." }
                });
            }

            if (doc.Sheets.Count == 0)
                doc.Sheets.Add(new ReportSheet
                {
                    Name = "INCENTIVES",
                    TitleLines = { "INCENTIVES", Period(r.From, r.To) },
                    Tables = { new ReportTable { Columns = { Col("DATE OF PAYMENT", 2, ColumnKind.Date), Col("DIVISION", 2, ColumnKind.Center), Col("DEPUTIZED", 4, ColumnKind.Text), Col("AMOUNT", 2, ColumnKind.Money), Col(rate, 2, ColumnKind.Money) }, EmptyMessage = "No payments recorded for this period." } }
                });

            return doc;
        }

        // ── DEPUTIZED ENFORCERS ──────────────────────────────────────────────
        public static ReportDocument Enforcers(EnforcerListDTO r)
        {
            var table = new ReportTable
            {
                Columns = { Col("NO", 0.8), Col("OFFICE", 1.6, ColumnKind.Center), Col("NAME", 6, ColumnKind.Text), Col("DEPUTATION", 2.4, ColumnKind.Center) }
            };
            var n = 0;
            foreach (var e in r.Rows)
            {
                n++;
                table.Rows.Add(new object?[] { n, e.Office, e.Name.ToUpperInvariant(), e.DeputationNumber });
            }

            return new ReportDocument
            {
                FileBaseName = "povis-deputized-enforcers",
                Sheets =
                {
                    new ReportSheet
                    {
                        Name = "DEPUTIZED ENFORCER",
                        TitleLines = { "DEPUTIZED ENFORCERS", $"As of {DateTime.UtcNow.AddHours(8):MMMM d, yyyy}" },
                        Tables = { table },
                        Notes = { $"TOTAL DEPUTIZED ENFORCERS: {r.Rows.Count:N0}" }
                    }
                }
            };
        }

        private static string JoinAnd(List<string> items) => items.Count switch
        {
            0 => string.Empty,
            1 => items[0],
            _ => string.Join(", ", items.Take(items.Count - 1)) + " and " + items[^1]
        };
    }
}
