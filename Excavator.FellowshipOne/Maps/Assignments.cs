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
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Excavator.F1
{
    /// <summary>
    /// Partial of F1Component that holds the Activity Assignments import
    /// </summary>
    partial class F1Component
    {
        /// <summary>
        /// Maps the notes.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        private void MapActivityAssignment( IQueryable<Row> tableData )
        {
            var lookupContext = new RockContext();
            var categoryService = new CategoryService( lookupContext );
            var personService = new PersonService( lookupContext );



            var noteList = new List<Note>();

            int completed = 0;
            int totalRows = tableData.Count();
            int percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, string.Format( "Verifying activity assignment import ({0:N0} found).", totalRows ) );
            foreach ( var row in tableData )
            {
                int? rlcId = row["RLC_ID"] as int?;
                int? individualId = row["Individual_ID"] as int?;
                int? personId = GetPersonAliasId( individualId );

                //filter where RLC_Id != null and Individual_Id > 0 (three of the first records are -1)
                if ( rlcId != null && individualId > 0 && personId != null )
                {
                    //check if person already exists as member.
                    int? activityId = row["Activity_ID"] as int?;
                    string activityID = Convert.ToString( activityId );
                    string rlcID = Convert.ToString( rlcId );

                    //check if RLC Group Exists and existingRLCGroup contains that group instance
                    var existingRLCGroup = new GroupService( lookupContext ).Queryable().Where( g => g.ForeignId == rlcID ).FirstOrDefault();

                    //Gets member group type role Id.   
                    int? memberGroupTypeRoleId = new GroupTypeService( lookupContext ).Queryable().Where( g => g.ForeignId == activityID ).FirstOrDefault().DefaultGroupRoleId;
                    if ( memberGroupTypeRoleId != null || memberGroupTypeRoleId != 0 )
                    {

                        //checks if member is already a part of that group.
                        int groupMemberExists = new GroupMemberService( lookupContext ).Queryable().Where( g => g.GroupId == existingRLCGroup.Id && g.PersonId == personId && g.GroupRoleId == memberGroupTypeRoleId ).Select( a => a.Id ).FirstOrDefault();

                        //If person is not already a member, they will be added as a member 
                        if ( groupMemberExists == 0 )
                        {
                            DateTime? assignmentDate = row["AssignmentDateTime"] as DateTime?;

                            //adds member
                            var connectGroupMember = new GroupMember();
                            connectGroupMember.IsSystem = false;
                            connectGroupMember.GroupId = existingRLCGroup.Id;
                            connectGroupMember.PersonId = (int)personId;
                            connectGroupMember.GroupRoleId = (int)memberGroupTypeRoleId; //will add them as a member
                            ReportProgress( 0, string.Format( "GroupId: {0}, GroupName: {3}, PersonID: {1}, GroupRoleId: {2}", connectGroupMember.GroupId, connectGroupMember.PersonId, connectGroupMember.GroupRoleId, existingRLCGroup.Name ) );

                            //Save Member - may not need to save here, but use a list instead. If using list, turn groupMemberExists into list and check from that as well as check for existing records in the db.
                            var rockContext = new RockContext();
                            rockContext.WrapTransaction( () =>
                            {
                                rockContext.Configuration.AutoDetectChangesEnabled = false;
                                rockContext.GroupMembers.Add( connectGroupMember );
                                rockContext.SaveChanges( DisableAudit );
                            } );
                            completed++;
                        }
                    }
                }
                if ( completed % percentage < 1 )
                {
                    int percentComplete = completed / percentage;
                    ReportProgress( percentComplete, string.Format( "Members Imported: {0} ({1}% complete). ", completed, percentComplete ) );
                }
                else if ( completed % ReportingNumber < 1 )
                {
                    ReportPartialProgress();
                }
            }
            ReportProgress( 100, string.Format( "Finished Activity Assignment import: {0:N0} assignments imported.", completed ) );
        }
    }
}
