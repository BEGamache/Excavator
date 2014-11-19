// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

using System;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using OrcaMDF.Core.MetaData;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Excavator.F1
{
    /// <summary>
    /// Partial of F1Component that holds the Attendance import methods
    /// </summary>
    partial class F1Component
    {
        /// <summary>
        /// Maps the group attendance.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <returns></returns>
        //private DateTime? StartDateTime { get; set;}
        private void MapGroupsAttendance( IQueryable<Row> tableData )
        {
            var lookupContext = new RockContext();
            int completed = 0;
            int totalRows = tableData.Count();
            int percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, string.Format( "Verifying Attendance import ({0:N0} found).", totalRows ) );

            var attendanceList = new List<Rock.Model.Attendance>();

            var groupService = new GroupService( lookupContext );
            var existingGroupList = new List<Group>();
            existingGroupList = groupService.Queryable().Where(g => g.ForeignId != null).ToList();

            foreach ( var row in tableData )
            {

                DateTime? startTime = row["StartDateTime"] as DateTime?;
                if ( startTime != null && startTime != DateTime.MinValue )
                {
                    int? groupId = row["GroupID"] as int?;
                    if ( existingGroupList.Find(g => g.ForeignId == groupId.ToString()) != null ) //making sure the group has been created.
                    {
                    DateTime startDateTime = (DateTime)startTime;
                    DateTime? endTime = row["EndDateTime"] as DateTime?;
                    int? met = row["Met"] as int?;
                    bool didAttend = true;
                    if ( met == 0 ) { didAttend = false; }
                    string comments = row["Comments"] as string;

                        var attendance = new Rock.Model.Attendance();
                        attendance.CreatedByPersonAliasId = ImportPersonAlias.Id;
                        attendance.ModifiedByPersonAliasId = ImportPersonAlias.Id;
                        attendance.CreatedDateTime = DateTime.Today;
                        attendance.ModifiedDateTime = DateTime.Today;
                        attendance.StartDateTime = startDateTime;
                        attendance.EndDateTime = endTime;
                        attendance.DidAttend = didAttend;
                        attendance.Note = comments;
                        attendance.CampusId = 1; //Campus is needed for attendance to show in attendance analysis.
                        attendance.GroupId = existingGroupList.Find( g => g.ForeignId == groupId.ToString() ).Id;

                        int? individualId = row["IndividualID"] as int?;

                        if ( individualId != null )
                        {
                            attendance.PersonAliasId = GetPersonAliasId( individualId );
                        }

                        ////look up Group
                        //int? groupId = row["GroupID"] as int?;

                        //Group group = existingGroupList.Where( g => g.ForeignId == ( groupId.ToString() ) ).FirstOrDefault();
                        //if ( group != null )
                        //{
                        //    attendance.GroupId = group.Id;
                        //}

                    //no other search type right now; small group attendance not currently set up in Rock.
                        var dvService = new DefinedValueService( lookupContext );

                        attendance.SearchTypeValueId = dvService.Queryable().Where( dv => dv.Value == "Phone Number" ).FirstOrDefault().Id;


                        completed++;
                        if ( completed % percentage < 1 )
                        {
                            int percentComplete = completed / percentage;
                            ReportProgress( percentComplete, string.Format( "Completed: {0:N0} Percent Completed: {0:N0} ", completed, percentComplete ) );
                        }


                        var rockContext = new RockContext();
                        rockContext.WrapTransaction( () =>
                        {
                            rockContext.Configuration.AutoDetectChangesEnabled = false;

                            rockContext.Attendances.Add( attendance );
                            rockContext.SaveChanges( DisableAudit );
                        } );


                        ReportPartialProgress();
                }
            }
            }
        }
    }
}
