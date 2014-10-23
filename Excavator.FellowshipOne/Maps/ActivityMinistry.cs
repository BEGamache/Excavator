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
        /// Maps the activity ministry.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <returns></returns>
        private void MapActivityMinistry( IQueryable<Row> tableData )
        {
            var lookupContext = new RockContext();
            var rockContext = new RockContext();

            // Add an Attribute for the unique F1 Ministry Id
            //int groupEntityTypeId = EntityTypeCache.Read("Rock.Model.Group").Id;
            //var ministryAttributeId = new AttributeService(lookupContext).Queryable().Where(a => a.EntityTypeId == groupEntityTypeId
            //    && a.Key == "F1MinistryId").Select(a => a.Id).FirstOrDefault();
            //var activityAttributeId = new AttributeService(lookupContext).Queryable().Where(a => a.EntityTypeId == groupEntityTypeId
            //    && a.Key == "F1ActivityId").Select(a => a.Id).FirstOrDefault();

            //if (ministryAttributeId == 0)
            //{
            //    var newMinistryAttribute = new Rock.Model.Attribute();
            //    newMinistryAttribute.Key = "F1MinistryId";
            //    newMinistryAttribute.Name = "F1 Ministry Id";
            //    newMinistryAttribute.FieldTypeId = IntegerFieldTypeId;
            //    newMinistryAttribute.EntityTypeId = groupEntityTypeId;
            //    newMinistryAttribute.EntityTypeQualifierValue = string.Empty;
            //    newMinistryAttribute.EntityTypeQualifierColumn = string.Empty;
            //    newMinistryAttribute.Description = "The FellowshipOne identifier for the ministry that was imported";
            //    newMinistryAttribute.DefaultValue = string.Empty;
            //    newMinistryAttribute.IsMultiValue = false;
            //    newMinistryAttribute.IsRequired = false;
            //    newMinistryAttribute.Order = 0;

            //    lookupContext.Attributes.Add(newMinistryAttribute);
            //    lookupContext.SaveChanges(DisableAudit);
            //    ministryAttributeId = newMinistryAttribute.Id;
            //}
            //if (activityAttributeId == 0)
            //{
            //    var newActivityAttribute = new Rock.Model.Attribute();
            //    newActivityAttribute.Key = "F1ActivityId";
            //    newActivityAttribute.Name = "F1 Activity Id";
            //    newActivityAttribute.FieldTypeId = IntegerFieldTypeId;
            //    newActivityAttribute.EntityTypeId = groupEntityTypeId;
            //    newActivityAttribute.EntityTypeQualifierValue = string.Empty;
            //    newActivityAttribute.EntityTypeQualifierColumn = string.Empty;
            //    newActivityAttribute.Description = "The FellowshipOne identifier for the activity that was imported";
            //    newActivityAttribute.DefaultValue = string.Empty;
            //    newActivityAttribute.IsMultiValue = false;
            //    newActivityAttribute.IsRequired = false;
            //    newActivityAttribute.Order = 0;

            //    lookupContext.Attributes.Add(newActivityAttribute);
            //    lookupContext.SaveChanges(DisableAudit);
            //    activityAttributeId = newActivityAttribute.Id;
            //}

            //// Get previously imported Ministries
            //List<AttributeValue> importedMinistriesAVList = new AttributeValueService(lookupContext).GetByAttributeId(ministryAttributeId).ToList();

            //// Get previously imported Activities
            //List<AttributeValue> importedActivitiesAVList = new AttributeValueService( lookupContext ).GetByAttributeId( activityAttributeId ).ToList();


            //int importedMinistriesCount = 0;
            //int importedActivitiesCount = 0;

            //if ( importedMinistriesAVList.Any() ) { importedMinistriesCount = importedMinistriesAVList.Count(); }
            //if ( importedActivitiesAVList.Any() ) { importedActivitiesCount = importedActivitiesAVList.Count(); }

            int completed = 0;
            int totalRows = tableData.Count();
            int percentage = ( totalRows - 1 ) / 100 + 1;

            ReportProgress( 0, string.Format( "Verifying ministry import ({0:N0} found).", totalRows ) );
            //ReportProgress(0, string.Format("Previously Imported Ministries ({0:N0} found).", importedMinistriesCount));
            //ReportProgress(0, string.Format("Previously Imported Activities ({0:N0} found).", importedActivitiesCount));

            var newAreas = new List<GroupType>();
            var newCategories = new List<GroupType>();

            foreach ( var row in tableData )
            {
                int? ministryId = row["Ministry_ID"] as int?;
                string ministryName = row["Ministry_Name"] as string;
                string ministryIdString = Convert.ToString( ministryId );

                //GroupType importedMinistriesGTList = new GroupTypeService( lookupContext ).Queryable().Where( g => g.Name == ministryName && g.ForeignId == ( Convert.ToString( ministryId ) + 'm' ) ).FirstOrDefault();
                int? importedMinistry = new GroupTypeService( lookupContext ).Queryable().Where( a => a.ForeignId == ministryIdString ).Select( a => a.Id ).FirstOrDefault();
                //AttributeValue importedMinistry = new AttributeValueService(lookupContext).Queryable().Where(a => a.Value == Convert.ToString(ministryId) && a.ForeignId == ministryName).FirstOrDefault();
                //AttributeValue importedMinistry = importedMinistriesAVList.Where(av => av.Value == Convert.ToString(ministryId)).FirstOrDefault();
                // if (ministryId != null && !importedMinistries.ContainsKey(ministryId)) //Checks AttributeValue table to see if it has already been imported.
                //if ( ministryId != null && importedMinistriesAVList.Find( x => x.Value == Convert.ToString( ministryId ) ) == null ) //Checks AttributeValue table to see if it has already been imported.
                if ( ministryId != null && importedMinistry == 0 ) //Checks AttributeValue table to see if it has already been imported.
                {

                    bool? ministryIsActive = row["Ministry_Active"] as bool?;

                    if ( ministryName != null )
                    {

                        var ministryCategory = new GroupType();

                        var ministryAttribute = new AttributeValue();
                        var ministryAttributeList = new List<AttributeValue>();

                        //Creates the GroupType data
                        ministryCategory.IsSystem = false;
                        ministryCategory.Name = ministryName.Trim();
                        ministryCategory.GroupTerm = "Group";
                        ministryCategory.GroupMemberTerm = "Member";
                        ministryCategory.AllowMultipleLocations = false;
                        ministryCategory.ShowInGroupList = true;
                        ministryCategory.ShowInNavigation = false;
                        ministryCategory.TakesAttendance = false;
                        ministryCategory.AttendanceRule = 0;
                        ministryCategory.AttendancePrintTo = 0;
                        ministryCategory.Order = 0;
                        ministryCategory.LocationSelectionMode = 0;
                        ministryCategory.Guid = Guid.NewGuid();
                        ministryCategory.ForeignId = ministryIdString; //F1 Ministry ID
                        ministryCategory.GroupTypePurposeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE ).Id; // ID = 142 in my db

                        //Creates the AttributeValue data for the Ministry
                        //ministryAttribute.IsSystem = false;
                        //ministryAttribute.AttributeId = ministryAttributeId;
                        //ministryAttribute.Value = ministryId.ToString();    //So the Value is the F1MinistryID
                        //ministryAttribute.Guid = Guid.NewGuid();
                        //ministryAttribute.ForeignId = ministryName.Trim();

                        newCategories.Add( ministryCategory );
                        //ministryAttributeList.Add(ministryAttribute);

                        //Saves it to the DB so that I can check for its ID in the table
                        if ( newCategories.Any() || ministryAttributeList.Any() )
                        {
                            //var rockContext = new RockContext();
                            if ( newCategories.Any() )
                            {
                                //var rockContext = new RockContext();
                                rockContext.WrapTransaction( () =>
                                {
                                    rockContext.Configuration.AutoDetectChangesEnabled = false;
                                    rockContext.GroupTypes.AddRange( newCategories );
                                    rockContext.SaveChanges( DisableAudit );
                                } );
                                newCategories.Clear();
                            }
                            //if ( ministryAttributeList.Any() )
                            //{
                            //    //var rockContext = new RockContext();
                            //    rockContext.WrapTransaction( () =>
                            //    {
                            //        rockContext.Configuration.AutoDetectChangesEnabled = false;
                            //        rockContext.AttributeValues.AddRange( ministryAttributeList );
                            //        rockContext.SaveChanges( DisableAudit );
                            //    } );
                            //    ministryAttributeList.Clear();
                            //}
                        }
                    }
                }




                //Checks AttributeValue table to see if it has already been imported.
                int? activityId = row["Activity_ID"] as int?;
                string activityName = row["Activity_Name"] as string;
                string activityIdString = Convert.ToString( activityId );
                ReportProgress( 0, string.Format( "Ministry ID {0}   Activity ID {1}.", ministryId, activityId ) );

                //if (activityId != null && !importedActivities.ContainsKey(activityId))
                int? importedActivity = new GroupTypeService( lookupContext ).Queryable().Where( a => a.ForeignId == activityIdString ).Select( a => a.Id ).FirstOrDefault();
                //AttributeValue importedActivity = new AttributeValueService( lookupContext ).Queryable().Where( a => a.Value == Convert.ToString( activityId ) && a.ForeignId == activityName ).FirstOrDefault();
                //AttributeValue importedActivity = importedActivitiesAVList.Where( av => av.Value == Convert.ToString( activityId ) ).FirstOrDefault();
                //if ( activityId != null && importedActivitiesAVList.Find(x => x.Value == Convert.ToString(activityId)) == null )
                if ( activityId != null && importedActivity == 0 )
                {


                    bool? activityIsActive = row["Activity_Active"] as bool?;

                    //Looking up the Ministry GroupType ID so it can be used as the ParentGroupTypeId for the Activity GroupType/Area
                    var gtService = new GroupTypeService( lookupContext );
                    int parentGroupTypeId;

                    string ministryID = ministryId.ToString();
                    parentGroupTypeId = gtService.Queryable().Where( gt => gt.ForeignId == ministryID ).FirstOrDefault().Id;

                    var parentGroupType = new GroupTypeService( rockContext ).Get( parentGroupTypeId );

                    var activityArea = new GroupType();
                    var activityAV = new AttributeValue();
                    var activityAVList = new List<AttributeValue>();


                    // create new GroupType for activity (will set this as child to Ministry/Category)
                    activityArea.IsSystem = false;
                    activityArea.Name = activityName.Trim();
                    activityArea.GroupTerm = "Group";
                    activityArea.GroupMemberTerm = "Member";

                    //Setup Group role Id
                    activityArea.DefaultGroupRole.Name = "Member";
                    activityArea.DefaultGroupRole.Description = activityName + " - Assignment";

                    activityArea.AllowMultipleLocations = true;
                    activityArea.ShowInGroupList = true;
                    activityArea.ShowInNavigation = false;
                    activityArea.TakesAttendance = true;
                    activityArea.AttendanceRule = 0;
                    activityArea.AttendancePrintTo = 0;
                    activityArea.Order = 0;
                    activityArea.LocationSelectionMode = 0;
                    activityArea.Guid = Guid.NewGuid();
                    activityArea.ForeignId = activityIdString;  //F1 Activity ID

                    //Sets GroupTypeAssociation for the Categories and Areas
                    activityArea.ParentGroupTypes = new List<GroupType>();
                    activityArea.ParentGroupTypes.Add( parentGroupType );


                    //Create Activity AttributeValue Data
                    //activityAV.IsSystem = false;
                    //activityAV.Guid = Guid.NewGuid();
                    //activityAV.AttributeId = activityAttributeId;
                    //activityAV.Value = activityId.ToString();
                    //activityAV.ForeignId = activityName;

                    //activityAVList.Add(activityAV);
                    newAreas.Add( activityArea );
                    completed++;

                    if ( newAreas.Any() || activityAVList.Any() )
                    {
                        //var rockContext = new RockContext();
                        if ( newAreas.Any() )
                        {
                            rockContext.WrapTransaction( () =>
                            {
                                rockContext.Configuration.AutoDetectChangesEnabled = false;
                                rockContext.GroupTypes.AddRange( newAreas );
                                rockContext.SaveChanges( DisableAudit );
                            } );
                            newAreas.Clear();
                        }
                        //if ( activityAVList.Any() )
                        //{
                        //    //var rockContext = new RockContext();
                        //    rockContext.WrapTransaction( () =>
                        //    {
                        //        rockContext.Configuration.AutoDetectChangesEnabled = false;
                        //        rockContext.AttributeValues.AddRange( activityAVList );
                        //        rockContext.SaveChanges( DisableAudit );
                        //    } );
                        //    activityAVList.Clear();
                        //}
                    }
                }
                if ( completed % percentage < 1 )
                {
                    int percentComplete = completed / percentage;
                    ReportProgress( percentComplete, string.Format( "{0:N0} ministries imported ({1}% complete). Categories: {2:N0} Areas: {3:N0}", completed, percentComplete, newCategories.Count, newAreas.Count ) );
                }
                else if ( completed % ReportingNumber < 1 )
                {
                    //var rockContext = new RockContext();
                    //rockContext.WrapTransaction(() =>
                    //{
                    //    rockContext.Configuration.AutoDetectChangesEnabled = false;
                    //    rockContext.GroupTypes.AddRange(newCategories);
                    //    rockContext.GroupTypes.AddRange(newAreas);
                    //    rockContext.SaveChanges(DisableAudit);
                    //});

                    ReportPartialProgress();
                }
            }
            //if (newAreas.Any())
            //{
            //    //var rockContext = new RockContext();
            //    rockContext.WrapTransaction(() =>
            //    {
            //        rockContext.Configuration.AutoDetectChangesEnabled = false;
            //        rockContext.GroupTypes.AddRange(newAreas);
            //        rockContext.SaveChanges(DisableAudit);
            //    });
            //}
            ReportProgress(100, string.Format("Finished ministry import: {0:N0} ministries imported. ", completed) );
            //ReportProgress( 0, string.Format( "Categories: {0}  Areas: {1}", importedMinistriesAVList.Count(), importedActivitiesAVList.Count() ) );

        }
    }
}