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
       /// Maps the attendance.
       /// </summary>
       /// <param name="tableData">The table data.</param>
       /// <returns></returns>
        //private DateTime? StartDateTime { get; set;}
        private void MapAttendance( IQueryable<Row> tableData )
        {
            var lookupContext = new RockContext();
            int completed = 0;
            int totalRows = tableData.Count();
            int percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, string.Format( "Verifying Attendance import ({0:N0} found).", totalRows ) );

            var attendanceList = new List<Rock.Model.Attendance>();

            foreach ( var row in tableData )
            {

                   DateTime? startTime = row["Start_Date_Time"] as DateTime?;
                if ( startTime != null && startTime != DateTime.MinValue)
                {
                    DateTime startDateTime = (DateTime)startTime;
                    if ( startDateTime.Year >= 2013 )
                    { 

                    //startDateTime = BruteForceDateTime(startTime);

                    var attendance = new Rock.Model.Attendance();
                    attendance.CreatedByPersonAliasId = ImportPersonAlias.Id;
                    attendance.ModifiedByPersonAliasId = ImportPersonAlias.Id;
                    attendance.CreatedDateTime = DateTime.Today;
                    attendance.ModifiedDateTime = DateTime.Today;
                    attendance.StartDateTime = startDateTime; //(DateTime)startTime; 
                    attendance.DidAttend = true;


                    //string position = row["CheckedInAs"] as string;
                    //string jobTitle = row["Job_Title"] as string;
                    //string machineName = row["Checkin_Machine_Name"] as string;
                    int? rlcId = row["RLC_ID"] as int?;

                    int? individualId = row["Individual_ID"] as int?;

                        if ( individualId != null )
                        {
                            attendance.PersonAliasId = GetPersonAliasId( individualId );
                        }

                        DateTime? checkInTime = row["Check_In_Time"] as DateTime?;
                        if ( checkInTime != null )
                        {
                            // set the start time to the time they actually checked in. If null it maintains Start_Date_Time
                            attendance.StartDateTime = (DateTime)checkInTime; //BruteForceDateTime( checkInTime );
                        }

                        DateTime? checkOutTime = row["Check_Out_Time"] as DateTime?;
                        if ( checkOutTime != null )
                        {
                            attendance.EndDateTime = (DateTime)checkOutTime; //BruteForceDateTime( checkOutTime );
                        }

                        //string f1AttendanceCode = row["Tag_Code"] as string;
                        //if ( f1AttendanceCode != null )
                        //{
                        //    attendance.AttendanceCode = new Rock.Model.AttendanceCode();
                        //    attendance.AttendanceCode.Code = f1AttendanceCode;
                        //}
                        string f1AttendanceCheckedInAs = row["CheckedInAs"] as string;
                        if ( f1AttendanceCheckedInAs != null )
                        {
                            attendance.Note = f1AttendanceCheckedInAs;
                        }


                        // look up location, schedule, and device -- all of these fields can be null if need be
                        attendance.LocationId = GetLocationId( Convert.ToInt32( rlcId ) );


                        //look up Group
                        var groupService = new GroupService( lookupContext );
                        var existingGroupList = new List<Group>();
                        existingGroupList = groupService.Queryable().ToList();

                        Group rlcGroup = existingGroupList.Where( g => g.ForeignId == ( rlcId.ToString() ) ).FirstOrDefault();
                        if ( rlcGroup != null )
                        {
                            attendance.GroupId = rlcGroup.Id;
                        }

                        var dvService = new DefinedValueService( lookupContext );

                        attendance.SearchTypeValueId = dvService.Queryable().Where( dv => dv.Value == "Phone Number" ).FirstOrDefault().Id;

                        //ReportProgress( 0, string.Format( "{0},{1},{2},{3},{4},{5},{6},{7},{8}", individualId,rlcId,rlcGroup.Name,attendance.CreatedByPersonAliasId,attendance.ModifiedByPersonAliasId,attendance.StartDateTime,attendance.DidAttend,attendance.AttendanceCode,attendance.LocationId ) );

                        //look into creating DeviceIds and Locations (Generic)


                        // Other Attributes to create:
                        // Tag_Comment
                        // BreakoutGroup_Name
                        // Pager_Code

                        //attendanceList.Add( attendance );


                        completed++;
                        if ( completed % percentage < 1 )
                        {
                            int percentComplete = completed / percentage;
                            ReportProgress( percentComplete, string.Format( "Completed: {0:N0} Percent Completed: {0:N0} ", completed, percentComplete ) );
                        }
                    //    else if ( completed % ReportingNumber < 1 )
                    //    {
                    //        var rockContext = new RockContext();
                    //        rockContext.WrapTransaction( () =>
                    //{
                    //    rockContext.Configuration.AutoDetectChangesEnabled = false;
                    //    rockContext.Attendances.AddRange( attendanceList );
                    //    rockContext.SaveChanges( DisableAudit );
                    //} );


                    //        ReportPartialProgress();
                    //    }


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


        private DateTime BruteForceDateTime(DateTime? oldtDateTime)
        {
            DateTime newDateTime = Convert.ToDateTime(oldtDateTime);
            int year = newDateTime.Year;
            int month = newDateTime.Month;
            int day = newDateTime.Day;
            int hour = newDateTime.Hour;
            int minute = newDateTime.Minute;
            int second = newDateTime.Second;

            DateTime newStartDateTime = new DateTime( year, month, day, hour, minute, second );
            return newStartDateTime;

            //throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the Location ID of a location already in Rock. --looks under dbo.group.foreignId then compares group.description with location name.
        /// </summary>
        /// <param name="rlcID">rlc ID </param>
        /// <returns>Location ID</returns>
        private int GetLocationId(int rlcId)
        {
            var lookupContext = new RockContext();
            var groupService = new GroupService(lookupContext);
            var rlcGroup = new Group();
            string rlcIdString = rlcId.ToString();
            rlcGroup = groupService.Queryable().Where(g => g.ForeignId == (rlcIdString)).FirstOrDefault();
            string groupLocation = String.Empty;
            if ( rlcGroup != null ) { groupLocation = rlcGroup.Description; }

            if (!String.IsNullOrWhiteSpace(groupLocation))
            {
                var locationService = new LocationService(lookupContext);
                var location = new List<Location>();
                location = locationService.Queryable().Where(l => l.ParentLocationId.HasValue).ToList();

                switch (groupLocation)
                {
                    case "A201":
                        {
                            return location.Where(l => l.Name == "A201").FirstOrDefault().Id;
                        }
                    case "A202":
                        {
                            return location.Where(l => l.Name == "A202").FirstOrDefault().Id;
                        }
                    case "Area X Basketball":
                        {
                            return location.Where(l => l.Name == "Area X").FirstOrDefault().Id;
                        }
                    case "Area X Main Area":
                        {
                            return location.Where(l => l.Name == "Area X").FirstOrDefault().Id;
                        }
                    case "Auditorium":
                        {
                            return location.Where(l => l.Name == "Auditorium").FirstOrDefault().Id;
                        }
                    case "Auditorium Recording Booth":
                        {
                            return location.Where(l => l.Name == "Auditorium").FirstOrDefault().Id;
                        }
                    case "Auditorium Sound Booth":
                        {
                            return location.Where(l => l.Name == "Auditorium").FirstOrDefault().Id;
                        }
                    case "Bookstore Downstairs":
                        {
                            return location.Where(l => l.Name == "Bookstore").FirstOrDefault().Id;
                        }
                    case "Bookstore Upstairs":
                        {
                            return location.Where(l => l.Name == "Bookstore").FirstOrDefault().Id;
                        }
                    case "Bug 117":
                        {
                            return location.Where(l => l.Name == "Bug").FirstOrDefault().Id;
                        }
                    case "Bunny 114":
                        {
                            return location.Where(l => l.Name == "Bunny").FirstOrDefault().Id;
                        }
                    case "Butterfly 108":
                        {
                            return location.Where(l => l.Name == "Butterfly").FirstOrDefault().Id;
                        }
                    case "C201":
                        {
                            return location.Where(l => l.Name == "C201").FirstOrDefault().Id;
                        }
                    case "C202":
                        {
                            return location.Where(l => l.Name == "C202").FirstOrDefault().Id;
                        }
                    case "C203":
                        {
                            return location.Where(l => l.Name == "C203").FirstOrDefault().Id;
                        }
                    case "Car 1":
                        {
                            return location.Where(l => l.Name == "Car 1").FirstOrDefault().Id;
                        }
                    case "Car 10":
                        {
                            return location.Where(l => l.Name == "Car 10").FirstOrDefault().Id;
                        }
                    case "Car 2":
                        {
                            return location.Where(l => l.Name == "Car 2").FirstOrDefault().Id;
                        }
                    case "Car 3":
                        {
                            return location.Where(l => l.Name == "Car 3").FirstOrDefault().Id;
                        }
                    case "Car 4":
                        {
                            return location.Where(l => l.Name == "Car 4").FirstOrDefault().Id;
                        }
                    case "Car 5":
                        {
                            return location.Where(l => l.Name == "Car 5").FirstOrDefault().Id;
                        }
                    case "Car 6":
                        {
                            return location.Where(l => l.Name == "Car 6").FirstOrDefault().Id;
                        }
                    case "Car 7":
                        {
                            return location.Where(l => l.Name == "Car 7").FirstOrDefault().Id;
                        }
                    case "Car 8":
                        {
                            return location.Where(l => l.Name == "Car 8").FirstOrDefault().Id;
                        }
                    case "Car 9":
                        {
                            return location.Where(l => l.Name == "Car 9").FirstOrDefault().Id;
                        }
                    case "Catapiller 107":
                        {
                            return location.Where( l => l.Name == "Caterpillar" ).FirstOrDefault().Id;
                        }
                    case "Chapel":
                        {
                            return location.Where(l => l.Name == "Chapel").FirstOrDefault().Id;
                        }
                    case "Chapel 101":
                        {
                            return location.Where(l => l.Name == "Chapel 101").FirstOrDefault().Id;
                        }
                    case "Chapel 102":
                        {
                            return location.Where( l => l.Name == "Chapel 102" ).FirstOrDefault().Id;
                        }
                    case "Chapel Hallway":
                        {
                            return location.Where(l => l.Name == "Chapel Entrance").FirstOrDefault().Id;
                        }
                    case "Chapel Sound Booth":
                        {
                            return location.Where(l => l.Name == "Chapel").FirstOrDefault().Id; 
                        }
                    //case "Children's Lobby":  //Kayla doesn't know what location this is
                    //    {
                    //        return location.Where(l => l.Name == "A201").FirstOrDefault().Id;
                    //    }
                    case "College House":
                        {
                            return location.Where(l => l.Name == "The College House").FirstOrDefault().Id;
                        }
                    case "Communion Prep Room":
                        {
                            return location.Where(l => l.Name == "Communion Prep Room").FirstOrDefault().Id;
                        }
                    case "Cross Street Classroom":
                        {
                            return location.Where(l => l.Name == "Cross Street").FirstOrDefault().Id;
                        }
                    case "Cross Street Main Area":
                        {
                            return location.Where(l => l.Name == "Cross Street").FirstOrDefault().Id;
                        }
                    case "Crossroads Station Registration":
                        {
                            return location.Where(l => l.Name == "Crossroads Station").FirstOrDefault().Id;
                        }
                    case "Decision Room A":
                        {
                            return location.Where(l => l.Name == "Decision Room - A").FirstOrDefault().Id;
                        }
                    case "Decision Room B":
                        {
                            return location.Where(l => l.Name == "Decision Room - B").FirstOrDefault().Id;
                        }
                    case "Decision Room C":
                        {
                            return location.Where( l => l.Name == "Decision Room - C" ).FirstOrDefault().Id;
                        }
                    case "Duck 116":
                        {
                            return location.Where(l => l.Name == "Duck").FirstOrDefault().Id;
                        }
                    case "Giggleville Hallway":
                        {
                            return location.Where(l => l.Name == "Giggleville").FirstOrDefault().Id;
                        }
                    case "Giggleville Registration":
                        {
                            return location.Where(l => l.Name == "Giggleville").FirstOrDefault().Id;
                        }
                    case "Grand Hall":
                        {
                            return location.Where(l => l.Name == "Grand Hall").FirstOrDefault().Id;
                        }
                    case "Grand Hall 105":
                        {
                            return location.Where(l => l.Name == "Grand Hall").FirstOrDefault().Id;
                        }
                    case "Helping Hands House":
                        {
                            return location.Where(l => l.Name == "Helping Hands").FirstOrDefault().Id;
                        }
                    case "Kitchen":
                        {
                            return location.Where(l => l.Name == "Kitchen").FirstOrDefault().Id;
                        }
                    case "Lamb 115":
                        {
                            return location.Where(l => l.Name == "Lamb").FirstOrDefault().Id;
                        }
                    case "Main Lobby":
                        {
                            return location.Where(l => l.Name == "Main Lobby").FirstOrDefault().Id;
                        }
                    case "Main Lobby Upstairs":
                        {
                            return location.Where(l => l.Name == "Main Lobby").FirstOrDefault().Id;
                        }
                    case "Music Suite Main Area":
                        {
                            return location.Where(l => l.Name == "Music Suite").FirstOrDefault().Id;
                        }
                    case "Music Suite Room B":
                        {
                            return location.Where(l => l.Name == "Music Suite").FirstOrDefault().Id;
                        }
                    case "North Lobby":
                        {
                            return location.Where(l => l.Name == "North Lobby").FirstOrDefault().Id;
                        }
                    case "North Lobby Upstairs":
                        {
                            return location.Where(l => l.Name == "North Lobby").FirstOrDefault().Id;
                        }
                    case "Parking Lot A":
                        {
                            return location.Where(l => l.Name == "Parking Lot A").FirstOrDefault().Id;
                        }
                    case "Parking Lot B":
                        {
                            return location.Where(l => l.Name == "Parking Lot B").FirstOrDefault().Id;
                        }
                    case "Parking Lot C":
                        {
                            return location.Where(l => l.Name == "Parking Lot C").FirstOrDefault().Id;
                        }
                    case "Parking Lot D":
                        {
                            return location.Where(l => l.Name == "Parking Lot D").FirstOrDefault().Id;
                        }
                    case "Patio 1A":
                        {
                            return location.Where(l => l.Name == "Patio 1A").FirstOrDefault().Id;
                        }
                    case "Patio 1B":
                        {
                            return location.Where(l => l.Name == "Patio 1B").FirstOrDefault().Id;
                        }
                    case "Patio 2A":
                        {
                            return location.Where(l => l.Name == "Patio 2A").FirstOrDefault().Id;
                        }
                    case "Patio 2B":
                        {
                            return location.Where(l => l.Name == "Patio 2B").FirstOrDefault().Id;
                        }
                    case "Patio 2C":
                        {
                            return location.Where(l => l.Name == "Patio 2C").FirstOrDefault().Id;
                        }
                    case "Patio 3A":
                        {
                            return location.Where(l => l.Name == "Patio 3A").FirstOrDefault().Id;
                        }
                    case "Patio 3B":
                        {
                            return location.Where(l => l.Name == "Patio 3B").FirstOrDefault().Id;
                        }
                    case "Patio 3C":
                        {
                            return location.Where(l => l.Name == "Patio 3C").FirstOrDefault().Id;
                        }
                    case "Patio 4A":
                        {
                            return location.Where(l => l.Name == "Patio 4A").FirstOrDefault().Id;
                        }
                    case "Patio 4B":
                        {
                            return location.Where(l => l.Name == "Patio 4B").FirstOrDefault().Id;
                        }
                    case "Prayer Room":
                        {
                            return location.Where(l => l.Name == "Prayer Room").FirstOrDefault().Id;
                        }
                    case "Puppy 118":
                        {
                            return location.Where(l => l.Name == "Puppy").FirstOrDefault().Id;
                        }
                    case "South Lobby":
                        {
                            return location.Where(l => l.Name == "South Lobby").FirstOrDefault().Id;
                        }
                    case "Sportcenter":
                        {
                            return location.Where(l => l.Name == "SportCenter").FirstOrDefault().Id;
                        }
                    case "Squirrel 113":
                        {
                            return location.Where(l => l.Name == "Squirrel").FirstOrDefault().Id;
                        }
                    case "Texas Hall - Dallas":
                        {
                            return location.Where(l => l.Name == "Dallas").FirstOrDefault().Id;
                        }
                    case "Texas Hall - Fort Worth":
                        {
                            return location.Where(l => l.Name == "Fort Worth").FirstOrDefault().Id;
                        }
                    case "Texas Hall - Houston":
                        {
                            return location.Where(l => l.Name == "Houston").FirstOrDefault().Id;
                        }
                    case "Texas Hall - San Antonio":
                        {
                            return location.Where(l => l.Name == "San Antonio").FirstOrDefault().Id;
                        }
                    case "Youth Cafe":
                        {
                            return location.Where(l => l.Name == "Doulos").FirstOrDefault().Id;
                        }
                    case "Youth Lobby":
                        {
                            return location.Where(l => l.Name == "Doulos").FirstOrDefault().Id;
                        }
                    case "Youth Main Area":
                        {
                            return location.Where(l => l.Name == "Doulos").FirstOrDefault().Id;
                        }
                    default:
                        return location.Where(l => l.Name == "Main Building").FirstOrDefault().Id;
               }
            }
            else
            {
                var locationService = new LocationService(lookupContext);
                var location = new Location();
                location = locationService.Queryable().Where(l => l.Name == "Main Building").FirstOrDefault();
                return location.Id;
            }

        }


        /// <summary>
        /// Saves the attendance.
        /// </summary>
        /// <param name="attendance">The attendance.</param>
        private static void SaveAttendance( Rock.Model.Attendance attendance )
        {
            var rockContext = new RockContext();
            rockContext.WrapTransaction( () =>
            {
                rockContext.Configuration.AutoDetectChangesEnabled = false;
                rockContext.Attendances.Add( attendance );
                rockContext.SaveChanges( DisableAudit );
            } );
        }

    }
}
