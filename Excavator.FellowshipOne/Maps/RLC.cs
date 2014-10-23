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
using System.Collections.Generic;
using System.Linq;
using OrcaMDF.Core.MetaData;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Excavator.F1
{
    partial class F1Component
    {
        /// <summary>
        /// Maps the RLC data to rooms, locations & classes -- Mapping RLC Names as groups under check-in
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <returns></returns>
        private void MapRLC( IQueryable<Row> tableData )
        {
            var lookupContext = new RockContext();

            // Add an Attribute for the unique F1 Ministry Id
            //int groupEntityTypeId = EntityTypeCache.Read("Rock.Model.Group").Id;
            //var rlcAttributeId = new AttributeService(lookupContext).Queryable().Where(a => a.EntityTypeId == groupEntityTypeId
            //    && a.Key == "F1RLCId").Select(a => a.Id).FirstOrDefault();

            //if (rlcAttributeId == 0)
            //{
            //    var newRLCAttribute = new Rock.Model.Attribute();
            //    newRLCAttribute.Key = "F1RLCId";
            //    newRLCAttribute.Name = "F1 RLC Id";
            //    newRLCAttribute.FieldTypeId = IntegerFieldTypeId;
            //    newRLCAttribute.EntityTypeId = groupEntityTypeId;
            //    newRLCAttribute.EntityTypeQualifierValue = string.Empty;
            //    newRLCAttribute.EntityTypeQualifierColumn = string.Empty;
            //    newRLCAttribute.Description = "The FellowshipOne identifier for the RLC Group that was imported";
            //    newRLCAttribute.DefaultValue = string.Empty;
            //    newRLCAttribute.IsMultiValue = false;
            //    newRLCAttribute.IsRequired = false;
            //    newRLCAttribute.Order = 0;

            //    lookupContext.Attributes.Add(newRLCAttribute);
            //    lookupContext.SaveChanges(DisableAudit);
            //    rlcAttributeId = newRLCAttribute.Id;
            //}

            // Get previously imported Ministries
            //var importedRLCs = new AttributeValueService(lookupContext).GetByAttributeId(rlcAttributeId)
            //    .Select(av => new { RLCId = av.Value.AsType<int?>(), RLCName = av.ForeignId })
            //    .ToDictionary(t => t.RLCId, t => t.RLCName);

            //List<AttributeValue> importedRLCAVList = new AttributeValueService( lookupContext ).GetByAttributeId( rlcAttributeId ).ToList(); //Not in use

            var newRLCGroupList = new List<Group>();

            var rlcAttributeValueList = new List<AttributeValue>();

            //Need a list of GroupTypes                              
            var gtService = new GroupTypeService( lookupContext );
            var existingGroupTypes = new List<GroupType>();
            existingGroupTypes = gtService.Queryable().ToList();

            int completed = 0;
            int totalRows = tableData.Count();
            int percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, string.Format( "Verifying RLC Group import ({0:N0} found).", totalRows ) );

            foreach ( var row in tableData )
            {
                int? rlcId = row["RLC_ID"] as int?;
                string rlcName = row["RLC_Name"] as string;
                string rlcStringId = Convert.ToString( rlcId );

                int? importedRLCs = new GroupService( lookupContext ).Queryable().Where( a => a.ForeignId == rlcStringId ).Select( a => a.Id ).FirstOrDefault();

                if ( rlcId != null && importedRLCs == 0 )
                {

                    if ( rlcName != null )
                    {
                        bool? rlcIsActive = row["Is_Active"] as bool?;
                        string roomName = row["Room_Name"] as string;
                        string maxCapacity = row["Max_Capacity"] as string;
                        string roomDescription = row["Room_Name"] as string;
                        string roomCode = row["Room_Code"] as string;
                        DateTime? startAgeDate = row["Start_Age_Date"] as DateTime?;
                        DateTime? endAgeDate = row["End_Age_Date"] as DateTime?;
                        int? activityId = row["Activity_ID"] as int?;

                        //Searches for Parent ActivityId
                        string activityStringId = activityId.ToString();
                        GroupType parentActivityArea = existingGroupTypes.Where( gt => gt.ForeignId == activityStringId ).FirstOrDefault();

                        if ( String.IsNullOrWhiteSpace( rlcName ) )
                        {
                            ReportProgress( 0, string.Format( "." ) );
                            rlcName = "Excavator Test";
                        }
                        var rlcGroup = new Group();
                        bool rlcIsActiveBool = (bool)rlcIsActive;

                        //Sets the Group values for RLC
                        rlcGroup.IsSystem = false;
                        rlcGroup.Name = rlcName.Trim();
                        rlcGroup.Order = 0;
                        //rlcGroup.Guid = new Guid();
                        rlcGroup.GroupTypeId = parentActivityArea.Id;
                        rlcGroup.IsActive = rlcIsActiveBool;
                        rlcGroup.Description = roomDescription;
                        rlcGroup.ForeignId = rlcStringId;
                        //rlcGroup.GroupLocations = new List<GroupLocation>();
                        //rlcGroup.GroupLocations.Add( CheckLocation( roomDescription ) );

                        var rlcAttributeValue = new AttributeValue();

                        //Sets the Attribute Values for RLC
                        //rlcAttributeValue.IsSystem = false;
                        //rlcAttributeValue.AttributeId = rlcAttributeId;
                        //rlcAttributeValue.Value = rlcStringId;
                        //rlcAttributeValue.ForeignId = rlcName.Trim();

                        //rlcAttributeValueList.Add(rlcAttributeValue);
                        newRLCGroupList.Add( rlcGroup );
                        completed++;

                        // Adds rlcGroup to newRLCGroup list
                        ReportProgress( 0, string.Format( "Parent Activity/Area: {1}  Group Added: {0}  IsActive: {2}.", rlcGroup.Name, parentActivityArea.Name, rlcGroup.IsActive ) );

                        if ( completed % percentage < 1 )
                        {
                            int percentComplete = completed / percentage;
                            ReportProgress( percentComplete, string.Format( "{0:N0} RLC/Groups imported ({1}% complete). ", completed, percentComplete ) );
                        }
                        else if ( completed % ReportingNumber < 1 )
                        {
                            var rockContext = new RockContext();
                            rockContext.WrapTransaction( () =>
                            {
                                rockContext.Configuration.AutoDetectChangesEnabled = false;
                                rockContext.AttributeValues.AddRange( rlcAttributeValueList );
                                rockContext.Groups.AddRange( newRLCGroupList );
                                rockContext.SaveChanges( DisableAudit );
                                newRLCGroupList.Clear();
                                rlcAttributeValueList.Clear();
                            } );


                            ReportPartialProgress();
                        }
                    }
                }
            }

            if ( newRLCGroupList.Any() )
            {
                var rockContext = new RockContext();
                rockContext.WrapTransaction( () =>
                {
                    rockContext.Configuration.AutoDetectChangesEnabled = false;
                    rockContext.AttributeValues.AddRange( rlcAttributeValueList );
                    rockContext.Groups.AddRange( newRLCGroupList );
                    rockContext.SaveChanges( DisableAudit );
                    newRLCGroupList.Clear();
                    rlcAttributeValueList.Clear();
                } );
            }
            ReportProgress( 100, string.Format( "Finished ministry and activity import: {0:N0} imported.", completed ) );
        }

        //private GroupLocation CheckLocation( string roomDescription )
        //{
        //    var lookupContext = new RockContext();

        //    if ( roomDescription != null ) { roomDescription = roomDescription.Trim(); }

        //    if ( !String.IsNullOrWhiteSpace( roomDescription ) )
        //    {
        //        var locationService = new LocationService( lookupContext );
        //        var location = new List<Location>();
        //        location = locationService.Queryable().Where( l => l.ParentLocationId.HasValue ).ToList();

        //        switch ( roomDescription )
        //        {
        //            case "A201":
        //                {
        //                    return location.Where( l => l.Name == "A201" ).FirstOrDefault().Id;
        //                }
        //            case "A202":
        //                {
        //                    return location.Where( l => l.Name == "A202" ).FirstOrDefault().Id;
        //                }
        //            case "Area X Basketball":
        //                {
        //                    return location.Where( l => l.Name == "Area X" ).FirstOrDefault().Id;
        //                }
        //            case "Area X Main Area":
        //                {
        //                    return location.Where( l => l.Name == "Area X" ).FirstOrDefault().Id;
        //                }
        //            case "Auditorium":
        //                {
        //                    return location.Where( l => l.Name == "Auditorium" ).FirstOrDefault().Id;
        //                }
        //            case "Auditorium Recording Booth":
        //                {
        //                    return location.Where( l => l.Name == "Auditorium" ).FirstOrDefault().Id;
        //                }
        //            case "Auditorium Sound Booth":
        //                {
        //                    return location.Where( l => l.Name == "Auditorium" ).FirstOrDefault().Id;
        //                }
        //            case "Bookstore Downstairs":
        //                {
        //                    return location.Where( l => l.Name == "Bookstore" ).FirstOrDefault().Id;
        //                }
        //            case "Bookstore Upstairs":
        //                {
        //                    return location.Where( l => l.Name == "Bookstore" ).FirstOrDefault().Id;
        //                }
        //            case "Bug 117":
        //                {
        //                    return location.Where( l => l.Name == "Bug" ).FirstOrDefault().Id;
        //                }
        //            case "Bunny 114":
        //                {
        //                    return location.Where( l => l.Name == "Bunny" ).FirstOrDefault().Id;
        //                }
        //            case "Butterfly 108":
        //                {
        //                    return location.Where( l => l.Name == "Butterfly" ).FirstOrDefault().Id;
        //                }
        //            case "C201":
        //                {
        //                    return location.Where( l => l.Name == "C201" ).FirstOrDefault().Id;
        //                }
        //            case "C202":
        //                {
        //                    return location.Where( l => l.Name == "C202" ).FirstOrDefault().Id;
        //                }
        //            case "C203":
        //                {
        //                    return location.Where( l => l.Name == "C203" ).FirstOrDefault().Id;
        //                }
        //            case "Car 1":
        //                {
        //                    return location.Where( l => l.Name == "Car 1" ).FirstOrDefault().Id;
        //                }
        //            case "Car 10":
        //                {
        //                    return location.Where( l => l.Name == "Car 10" ).FirstOrDefault().Id;
        //                }
        //            case "Car 2":
        //                {
        //                    return location.Where( l => l.Name == "Car 2" ).FirstOrDefault().Id;
        //                }
        //            case "Car 3":
        //                {
        //                    return location.Where( l => l.Name == "Car 3" ).FirstOrDefault().Id;
        //                }
        //            case "Car 4":
        //                {
        //                    return location.Where( l => l.Name == "Car 4" ).FirstOrDefault().Id;
        //                }
        //            case "Car 5":
        //                {
        //                    return location.Where( l => l.Name == "Car 5" ).FirstOrDefault().Id;
        //                }
        //            case "Car 6":
        //                {
        //                    return location.Where( l => l.Name == "Car 6" ).FirstOrDefault().Id;
        //                }
        //            case "Car 7":
        //                {
        //                    return location.Where( l => l.Name == "Car 7" ).FirstOrDefault().Id;
        //                }
        //            case "Car 8":
        //                {
        //                    return location.Where( l => l.Name == "Car 8" ).FirstOrDefault().Id;
        //                }
        //            case "Car 9":
        //                {
        //                    return location.Where( l => l.Name == "Car 9" ).FirstOrDefault().Id;
        //                }
        //            case "Catapiller 107":
        //                {
        //                    return location.Where( l => l.Name == "Caterpillar" ).FirstOrDefault().Id;
        //                }
        //            case "Chapel":
        //                {
        //                    return location.Where( l => l.Name == "Chapel" ).FirstOrDefault().Id;
        //                }
        //            case "Chapel 101":
        //                {
        //                    return location.Where( l => l.Name == "Chapel 101" ).FirstOrDefault().Id;
        //                }
        //            case "Chapel 102":
        //                {
        //                    return location.Where( l => l.Name == "Chapel 102" ).FirstOrDefault().Id;
        //                }
        //            case "Chapel Hallway":
        //                {
        //                    return location.Where( l => l.Name == "Chapel Entrance" ).FirstOrDefault().Id;
        //                }
        //            case "Chapel Sound Booth":
        //                {
        //                    return location.Where( l => l.Name == "Chapel" ).FirstOrDefault().Id;
        //                }
        //            //case "Children's Lobby":  //Kayla doesn't know what location this is
        //            //    {
        //            //        return location.Where(l => l.Name == "A201").FirstOrDefault().Id;
        //            //    }
        //            case "College House":
        //                {
        //                    return location.Where( l => l.Name == "The College House" ).FirstOrDefault().Id;
        //                }
        //            case "Communion Prep Room":
        //                {
        //                    return location.Where( l => l.Name == "Communion Prep Room" ).FirstOrDefault().Id;
        //                }
        //            case "Cross Street Classroom":
        //                {
        //                    return location.Where( l => l.Name == "Cross Street" ).FirstOrDefault().Id;
        //                }
        //            case "Cross Street Main Area":
        //                {
        //                    return location.Where( l => l.Name == "Cross Street" ).FirstOrDefault().Id;
        //                }
        //            case "Crossroads Station Registration":
        //                {
        //                    return location.Where( l => l.Name == "Crossroads Station" ).FirstOrDefault().Id;
        //                }
        //            case "Decision Room A":
        //                {
        //                    return location.Where( l => l.Name == "Decision Room - A" ).FirstOrDefault().Id;
        //                }
        //            case "Decision Room B":
        //                {
        //                    return location.Where( l => l.Name == "Decision Room - B" ).FirstOrDefault().Id;
        //                }
        //            case "Decision Room C":
        //                {
        //                    return location.Where( l => l.Name == "Decision Room - C" ).FirstOrDefault().Id;
        //                }
        //            case "Duck 116":
        //                {
        //                    return location.Where( l => l.Name == "Duck" ).FirstOrDefault().Id;
        //                }
        //            case "Giggleville Hallway":
        //                {
        //                    return location.Where( l => l.Name == "Giggleville" ).FirstOrDefault().Id;
        //                }
        //            case "Giggleville Registration":
        //                {
        //                    return location.Where( l => l.Name == "Giggleville" ).FirstOrDefault().Id;
        //                }
        //            case "Grand Hall":
        //                {
        //                    return location.Where( l => l.Name == "Grand Hall" ).FirstOrDefault().Id;
        //                }
        //            case "Grand Hall 105":
        //                {
        //                    return location.Where( l => l.Name == "Grand Hall" ).FirstOrDefault().Id;
        //                }
        //            case "Helping Hands House":
        //                {
        //                    return location.Where( l => l.Name == "Helping Hands" ).FirstOrDefault().Id;
        //                }
        //            case "Kitchen":
        //                {
        //                    return location.Where( l => l.Name == "Kitchen" ).FirstOrDefault().Id;
        //                }
        //            case "Lamb 115":
        //                {
        //                    return location.Where( l => l.Name == "Lamb" ).FirstOrDefault().Id;
        //                }
        //            case "Main Lobby":
        //                {
        //                    return location.Where( l => l.Name == "Main Lobby" ).FirstOrDefault().Id;
        //                }
        //            case "Main Lobby Upstairs":
        //                {
        //                    return location.Where( l => l.Name == "Main Lobby" ).FirstOrDefault().Id;
        //                }
        //            case "Music Suite Main Area":
        //                {
        //                    return location.Where( l => l.Name == "Music Suite" ).FirstOrDefault().Id;
        //                }
        //            case "Music Suite Room B":
        //                {
        //                    return location.Where( l => l.Name == "Music Suite" ).FirstOrDefault().Id;
        //                }
        //            case "North Lobby":
        //                {
        //                    return location.Where( l => l.Name == "North Lobby" ).FirstOrDefault().Id;
        //                }
        //            case "North Lobby Upstairs":
        //                {
        //                    return location.Where( l => l.Name == "North Lobby" ).FirstOrDefault().Id;
        //                }
        //            case "Parking Lot A":
        //                {
        //                    return location.Where( l => l.Name == "Parking Lot A" ).FirstOrDefault().Id;
        //                }
        //            case "Parking Lot B":
        //                {
        //                    return location.Where( l => l.Name == "Parking Lot B" ).FirstOrDefault().Id;
        //                }
        //            case "Parking Lot C":
        //                {
        //                    return location.Where( l => l.Name == "Parking Lot C" ).FirstOrDefault().Id;
        //                }
        //            case "Parking Lot D":
        //                {
        //                    return location.Where( l => l.Name == "Parking Lot D" ).FirstOrDefault().Id;
        //                }
        //            case "Patio 1A":
        //                {
        //                    return location.Where( l => l.Name == "Patio 1A" ).FirstOrDefault().Id;
        //                }
        //            case "Patio 1B":
        //                {
        //                    return location.Where( l => l.Name == "Patio 1B" ).FirstOrDefault().Id;
        //                }
        //            case "Patio 2A":
        //                {
        //                    return location.Where( l => l.Name == "Patio 2A" ).FirstOrDefault().Id;
        //                }
        //            case "Patio 2B":
        //                {
        //                    return location.Where( l => l.Name == "Patio 2B" ).FirstOrDefault().Id;
        //                }
        //            case "Patio 2C":
        //                {
        //                    return location.Where( l => l.Name == "Patio 2C" ).FirstOrDefault().Id;
        //                }
        //            case "Patio 3A":
        //                {
        //                    return location.Where( l => l.Name == "Patio 3A" ).FirstOrDefault().Id;
        //                }
        //            case "Patio 3B":
        //                {
        //                    return location.Where( l => l.Name == "Patio 3B" ).FirstOrDefault().Id;
        //                }
        //            case "Patio 3C":
        //                {
        //                    return location.Where( l => l.Name == "Patio 3C" ).FirstOrDefault().Id;
        //                }
        //            case "Patio 4A":
        //                {
        //                    return location.Where( l => l.Name == "Patio 4A" ).FirstOrDefault().Id;
        //                }
        //            case "Patio 4B":
        //                {
        //                    return location.Where( l => l.Name == "Patio 4B" ).FirstOrDefault().Id;
        //                }
        //            case "Prayer Room":
        //                {
        //                    return location.Where( l => l.Name == "Prayer Room" ).FirstOrDefault().Id;
        //                }
        //            case "Puppy 118":
        //                {
        //                    return location.Where( l => l.Name == "Puppy" ).FirstOrDefault().Id;
        //                }
        //            case "South Lobby":
        //                {
        //                    return location.Where( l => l.Name == "South Lobby" ).FirstOrDefault().Id;
        //                }
        //            case "Sportcenter":
        //                {
        //                    return location.Where( l => l.Name == "SportCenter" ).FirstOrDefault().Id;
        //                }
        //            case "Squirrel 113":
        //                {
        //                    return location.Where( l => l.Name == "Squirrel" ).FirstOrDefault().Id;
        //                }
        //            case "Texas Hall - Dallas":
        //                {
        //                    return location.Where( l => l.Name == "Dallas" ).FirstOrDefault().Id;
        //                }
        //            case "Texas Hall - Fort Worth":
        //                {
        //                    return location.Where( l => l.Name == "Fort Worth" ).FirstOrDefault().Id;
        //                }
        //            case "Texas Hall - Houston":
        //                {
        //                    return location.Where( l => l.Name == "Houston" ).FirstOrDefault().Id;
        //                }
        //            case "Texas Hall - San Antonio":
        //                {
        //                    return location.Where( l => l.Name == "San Antonio" ).FirstOrDefault().Id;
        //                }
        //            case "Youth Cafe":
        //                {
        //                    return location.Where( l => l.Name == "Doulos" ).FirstOrDefault().Id;
        //                }
        //            case "Youth Lobby":
        //                {
        //                    return location.Where( l => l.Name == "Doulos" ).FirstOrDefault().Id;
        //                }
        //            case "Youth Main Area":
        //                {
        //                    return location.Where( l => l.Name == "Doulos" ).FirstOrDefault().Id;
        //                }
        //            default:
        //                return location.Where( l => l.Name == "Main Building" ).FirstOrDefault().Id;
        //        }
        //    }
        //    else
        //    {
        //        var locationService = new LocationService( lookupContext );
        //        var location = new Location();
        //        location = locationService.Queryable().Where( l => l.Name == "Main Building" ).FirstOrDefault();
        //        return location.Id;
        //    }

        //} //Not going to work out
    }
}