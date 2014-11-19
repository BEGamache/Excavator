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
        /// Maps the family address.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <returns></returns>
        private void MapFamilyAddress( IQueryable<Row> tableData )
        {
            var lookupContext = new RockContext();
            var locationService = new LocationService( lookupContext );

            List<GroupMember> groupMembershipList = new GroupMemberService( lookupContext ).Queryable().Where( gm => gm.Group.GroupType.Guid == new Guid( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY ) ).ToList();

            var groupLocationTypeList = DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.GROUP_LOCATION_TYPE ), lookupContext ).DefinedValues;
            int homeGroupLocationTypeId = groupLocationTypeList.FirstOrDefault( dv => dv.Guid == new Guid( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME ) ).Id;
            int workGroupLocationTypeId = groupLocationTypeList.FirstOrDefault( dv => dv.Guid == new Guid( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_WORK ) ).Id;
            int previousGroupLocationTypeId = groupLocationTypeList.FirstOrDefault( dv => dv.Guid == new Guid( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS ) ).Id;

            var newGroupLocations = new List<GroupLocation>();
            var householdAVList = new AttributeValueService( lookupContext ).Queryable().Where( av => av.AttributeId == HouseholdAttributeId ).ToList();

            int completed = 0;
            int totalRows = tableData.Count();
            int percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, string.Format( "Verifying address import ({0:N0} found).", totalRows ) );

            foreach ( var row in tableData )
            {
                int? individualId = row["Individual_ID"] as int?; //null
                int? householdId = row["Household_ID"] as int?;
                //int? associatedPersonId = GetPersonAliasId( individualId, householdId ); //first person it will find is a visitor or child
                int? associatedPersonId;
                if ( individualId != null ) { associatedPersonId = GetPersonAliasId( individualId, householdId ); } //will get the exact person if Individual Id is not null.
                else { associatedPersonId = GetPersonId( householdAVList, householdId ); }

                if ( associatedPersonId != null ) //Will choose Head first, then Spouse, then Child (will disregard visitor and other)
                {
                    var familyGroup = groupMembershipList.Where( gm => gm.PersonId == (int)associatedPersonId )
                        .Select( gm => gm.Group ).FirstOrDefault(); //will assign this household address to this person's family and not the originating family.

                    if ( familyGroup != null )
                    {
                        var groupLocation = new GroupLocation();

                        string street1 = row["Address_1"] as string;
                        string street2 = row["Address_2"] as string;
                        string city = row["City"] as string;
                        string state = row["State"] as string;
                        string country = row["country"] as string; // NOT A TYPO: F1 has property in lower-case
                        string zip = row["Postal_Code"] as string;

                        Location familyAddress = locationService.Get( street1, street2, city, state, zip, country );

                        if ( familyAddress != null )
                        {
                            familyAddress.CreatedByPersonAliasId = ImportPersonAlias.Id;
                            familyAddress.Name = familyGroup.Name;
                            familyAddress.IsActive = true;

                            groupLocation.GroupId = familyGroup.Id;
                            groupLocation.LocationId = familyAddress.Id;
                            groupLocation.IsMailingLocation = true;
                            groupLocation.IsMappedLocation = true;

                            string addressType = row["Address_Type"].ToString().ToLower();
                            if ( addressType.Equals( "primary" ) )
                            {
                                groupLocation.GroupLocationTypeValueId = homeGroupLocationTypeId;
                            }
                            else if ( addressType.Equals( "business" ) || addressType.ToLower().Equals( "org" ) )
                            {
                                groupLocation.GroupLocationTypeValueId = workGroupLocationTypeId;
                            }
                            else if ( addressType.Equals( "previous" ) )
                            {
                                groupLocation.GroupLocationTypeValueId = previousGroupLocationTypeId;
                            }
                            else if ( !string.IsNullOrEmpty( addressType ) )
                            {
                                var customTypeId = groupLocationTypeList.Where( dv => dv.Value.ToLower().Equals( addressType ) )
                                    .Select( dv => (int?)dv.Id ).FirstOrDefault();
                                groupLocation.GroupLocationTypeValueId = customTypeId ?? homeGroupLocationTypeId;
                            }

                            newGroupLocations.Add( groupLocation );
                            completed++;

                            if ( completed % percentage < 1 )
                            {
                                int percentComplete = completed / percentage;
                                ReportProgress( percentComplete, string.Format( "{0:N0} addresses imported ({1}% complete).", completed, percentComplete ) );
                            }
                            else if ( completed % ReportingNumber < 1 )
                            {
                                SaveFamilyAddress( newGroupLocations );

                                // Reset context
                                newGroupLocations.Clear();
                                lookupContext = new RockContext();
                                locationService = new LocationService( lookupContext );

                                ReportPartialProgress();
                            }
                        }
                    }
                }
            }

            if ( newGroupLocations.Any() )
            {
                SaveFamilyAddress( newGroupLocations );
            }

            ReportProgress( 100, string.Format( "Finished address import: {0:N0} addresses imported.", completed ) );
        }

        private int? GetPersonId(List<AttributeValue> householdAVList, int? householdId)
        {
            var lookupContext = new RockContext();

            int? associatedPersonId = null;
            
            var head = householdAVList.FirstOrDefault( p => p.Value == householdId.ToString() && p.ForeignId == "head" );
            if ( head == null )
            {
                var spouse = householdAVList.FirstOrDefault( p => p.Value == householdId.ToString() && p.ForeignId == "spouse" );
                if ( spouse == null )
                {
                    var child = householdAVList.FirstOrDefault( p => p.Value == householdId.ToString() && p.ForeignId == "child" );
                    if ( child == null )
                    {
                        var business = householdAVList.FirstOrDefault( p => p.Value == householdId.ToString() && p.ForeignId == "business" );
                        if ( business == null )
                        {
                            var other = householdAVList.FirstOrDefault( p => p.Value == householdId.ToString() && p.ForeignId == "other" );
                            if ( other == null )
                            {
                                var visitor = householdAVList.FirstOrDefault( p => p.Value == householdId.ToString() && p.ForeignId == "visitor" );
                                if ( visitor != null )
                                {
                                    return associatedPersonId = visitor.EntityId;
                                }
                            }
                            else
                            {
                                return associatedPersonId = other.EntityId;
                            }
                        }
                        else
                        {
                            return associatedPersonId = business.EntityId;
                        }
                    }
                    else
                    {
                        return associatedPersonId = child.EntityId;
                    }
                }
                else
                {
                   return associatedPersonId = spouse.EntityId;
                }
            }
            else
            {
               return associatedPersonId = head.EntityId;
            }
            return associatedPersonId; 
        }

        /// <summary>
        /// Saves the family address.
        /// </summary>
        /// <param name="newGroupLocations">The new group locations.</param>
        private static void SaveFamilyAddress( List<GroupLocation> newGroupLocations )
        {
            var rockContext = new RockContext();
            rockContext.WrapTransaction( () =>
            {
                rockContext.Configuration.AutoDetectChangesEnabled = false;
                rockContext.GroupLocations.AddRange( newGroupLocations );
                rockContext.SaveChanges( DisableAudit );
            } );
        }
    }
}
