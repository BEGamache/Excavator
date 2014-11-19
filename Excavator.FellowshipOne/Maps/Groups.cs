// <copyright> new
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
    /// <summary>
    /// Partial of F1Component that holds the People import methods
    /// </summary>
    partial class F1Component
    {
        /// <summary>
        /// Maps the Connect Groups.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <returns></returns>
        private void MapGroups( IQueryable<Row> tableData ) //Just mapping Connect Groups and not People Lists (Wonder if People lists could be Tags?)
        {
            var lookupContext = new RockContext();
            int completedMembers = 0;
            int completedGroups = 0;
            int completedLifeStages = 0;
            int completedTags = 0;
            int completedIndividualTags = 0;
            int totalRows = tableData.Count();
            int percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, string.Format( "Verifying group import ({0:N0} found. Total may vary based on Group Type Name).", totalRows ) );


            foreach ( var row in tableData )
            {
                var rockContext = new RockContext();
                var lifeStageContext = new RockContext();
                var connectGroupContext = new RockContext();
                var connectGroupMemberContext = new RockContext();

                string groupTypeName = row["Group_Type_Name"] as string;
                if ( groupTypeName.Trim() == "Connect Groups" )            //Moves Connect Groups into Rock Groups
                {

                    var groupTypeIdSection = new GroupTypeService( lookupContext ).Queryable().Where( gt => gt.Name == "Event/Serving/Small Group Section" ).Select( a => a.Id ).FirstOrDefault();
                    var connectGroupsId = new GroupService( lookupContext ).Queryable().Where( g => g.Name == "Connect Groups" && g.GroupTypeId == groupTypeIdSection ).Select( a => a.Id ).FirstOrDefault();
                    var groupTypeIdSmallGroup = new GroupTypeService( lookupContext ).Queryable().Where( gt => gt.Name == "Small Group" ).Select( a => a.Id ).FirstOrDefault();

                    string groupName = row["Group_Name"] as string;
                    int? groupId = row["Group_ID"] as int?;
                    int? individualId = row["Individual_ID"] as int?;
                    int? personId = GetPersonAliasId( individualId );
                    DateTime? createdDateTime = row["Created_Date"] as DateTime?;


                    //Check to see if Head of Connect Group Tree exists

                    //If it doesn't exist
                    if ( connectGroupsId == 0 )
                    {
                        //Create one.
                        var connectGroupTree = new Group();
                        connectGroupTree.IsSystem = false;
                        connectGroupTree.GroupTypeId = groupTypeIdSection;
                        connectGroupTree.CampusId = 1;
                        connectGroupTree.Name = "Connect Groups";
                        connectGroupTree.Description = "Crossroads Connect Group Ministry";
                        connectGroupTree.IsActive = true;
                        //connectGroupTree.Order = 0;
                        connectGroupTree.CreatedByPersonAliasId = 1;
                        connectGroupTree.CreatedDateTime = DateTime.Now;

                        //save group
                        rockContext.WrapTransaction( () =>
                        {
                            rockContext.Configuration.AutoDetectChangesEnabled = false;
                            rockContext.Groups.Add( connectGroupTree );
                            rockContext.SaveChanges( DisableAudit );
                        } );
                    }

                    //check to see if life stage exists
                    //getting the life stage name
                    string lifeStage = groupName;
                    int index = lifeStage.IndexOf( "-" );
                    if ( index > 0 )
                        lifeStage = lifeStage.Substring( 0, index ).Trim();

                    //checks to see if it exists
                    int existingLifeStage = new GroupService( lookupContext ).Queryable().Where( g => g.Name == lifeStage ).Select( a => a.Id ).FirstOrDefault();
                    if ( existingLifeStage == 0 )
                    {
                        //Create one.
                        var connectGroupsLifeStage = new Group();
                        connectGroupsLifeStage.IsSystem = false;
                        connectGroupsLifeStage.ParentGroupId = connectGroupsId;
                        connectGroupsLifeStage.GroupTypeId = groupTypeIdSection;
                        connectGroupsLifeStage.CampusId = 1;
                        connectGroupsLifeStage.Name = lifeStage;
                        connectGroupsLifeStage.Description = "";
                        connectGroupsLifeStage.IsActive = true;
                        //connectGroupsLifeStage.Order = 0;
                        connectGroupsLifeStage.CreatedByPersonAliasId = 1;
                        connectGroupsLifeStage.CreatedDateTime = DateTime.Now;

                        //save Life Stage

                        lifeStageContext.WrapTransaction( () =>
                        {
                            lifeStageContext.Configuration.AutoDetectChangesEnabled = false;
                            lifeStageContext.Groups.Add( connectGroupsLifeStage );
                            lifeStageContext.SaveChanges( DisableAudit );
                        } );
                        completedLifeStages++;
                    }

                    int existingConnectGroup = new GroupService( lookupContext ).Queryable().Where( g => g.Name == groupName ).Select( a => a.Id ).FirstOrDefault();
                    existingLifeStage = new GroupService( lookupContext ).Queryable().Where( g => g.Name == lifeStage ).Select( a => a.Id ).FirstOrDefault();
                    //check to see if Connect Group exists.
                    if ( existingConnectGroup == 0 )
                    {
                        //Create one.
                        var connectGroups = new Group();
                        connectGroups.IsSystem = false;
                        connectGroups.GroupTypeId = groupTypeIdSmallGroup;
                        connectGroups.ParentGroupId = existingLifeStage;
                        connectGroups.CampusId = 1;
                        connectGroups.Name = groupName;
                        connectGroups.Description = "";
                        connectGroups.IsActive = true;
                        //connectGroups.Order = 0;
                        connectGroups.CreatedByPersonAliasId = 1;
                        connectGroups.CreatedDateTime = createdDateTime;
                        connectGroups.ForeignId = groupId.ToString(); //Will use this for GroupsAttendance

                        //Save Group
                        connectGroupContext.WrapTransaction( () =>
                        {
                            connectGroupContext.Configuration.AutoDetectChangesEnabled = false;
                            connectGroupContext.Groups.Add( connectGroups );
                            connectGroupContext.SaveChanges( DisableAudit );
                        } );
                        completedGroups++;
                    }

                    existingConnectGroup = new GroupService( lookupContext ).Queryable().Where( g => g.Name == groupName ).Select( a => a.Id ).FirstOrDefault();
                    
                    //Adds Group Member(s)
                    //makes sure Connect Group Exists
                    if ( existingConnectGroup != 0 )
                    {
                        int memberGroupTypeRoleId = new GroupTypeRoleService( lookupContext ).Queryable().Where( g => g.GroupTypeId == groupTypeIdSmallGroup && g.Name == "Member" ).Select( a => a.Id ).FirstOrDefault();
                        int groupMemberExists = new GroupMemberService( lookupContext ).Queryable().Where( g => g.GroupId == existingConnectGroup && g.PersonId == personId && g.GroupRoleId == memberGroupTypeRoleId ).Select( a => a.Id ).FirstOrDefault();
                        if ( groupMemberExists == 0 )
                        {
                            //adds member
                            var connectGroupMember = new GroupMember();
                            connectGroupMember.IsSystem = false;
                            connectGroupMember.GroupId = existingConnectGroup;
                            connectGroupMember.PersonId = (int)personId;
                            connectGroupMember.GroupRoleId = memberGroupTypeRoleId; //will add them as a member
                           // ReportProgress( 0, string.Format( "GroupId: {0}, GroupName: {3}, PersonID: {1}, GroupRoleId: {2}", connectGroupMember.GroupId, connectGroupMember.PersonId, connectGroupMember.GroupRoleId, groupName ) );

                            //Save Member
                            connectGroupMemberContext.WrapTransaction( () =>
                            {
                                connectGroupMemberContext.Configuration.AutoDetectChangesEnabled = false;
                                connectGroupMemberContext.GroupMembers.Add( connectGroupMember );
                                connectGroupMemberContext.SaveChanges( DisableAudit );
                            } );
                            completedMembers++;
                        }
                    }

                    if ( completedMembers % percentage < 1 )
                    {
                        int percentComplete = completedMembers / percentage;
                        //ReportProgress( percentComplete, string.Format( "Life Stages Imported: {0}, Groups Imported: {1}, Members Imported: {2} ({3}% complete). ", completedLifeStages, completedGroups, completedMembers, percentComplete ) );
                    }
                    else if ( completedMembers % ReportingNumber < 1 )
                    {
                        ReportPartialProgress();
                    }

                }


                if ( groupTypeName.Trim() == "Care Ministries" )            //Moves Care Ministries into Rock Groups
                {

                    var groupTypeIdSection = new GroupTypeService( lookupContext ).Queryable().Where( gt => gt.Name == "Event/Serving/Small Group Section" ).Select( a => a.Id ).FirstOrDefault();
                    var careMinistriesId = new GroupService( lookupContext ).Queryable().Where( g => g.Name == "Care Ministries" && g.GroupTypeId == groupTypeIdSection ).Select( a => a.Id ).FirstOrDefault();
                    var groupTypeIdSmallGroup = new GroupTypeService( lookupContext ).Queryable().Where( gt => gt.Name == "Small Group" ).Select( a => a.Id ).FirstOrDefault();

                    string groupName = row["Group_Name"] as string;
                    int? groupId = row["Group_ID"] as int?;
                    int? individualId = row["Individual_ID"] as int?;
                    int? personId = GetPersonAliasId( individualId );
                    DateTime? createdDateTime = row["Created_Date"] as DateTime?;


                    //Check to see if Head of Care Ministries Tree exists

                    //If it doesn't exist
                    if ( careMinistriesId == 0 )
                    {
                        //Create one.
                        var connectGroupTree = new Group();
                        connectGroupTree.IsSystem = false;
                        connectGroupTree.GroupTypeId = groupTypeIdSection;
                        connectGroupTree.CampusId = 1;
                        connectGroupTree.Name = "Care Ministries";
                        connectGroupTree.Description = "Crossroads Care Ministries";
                        connectGroupTree.IsActive = true;
                        //connectGroupTree.Order = 0;
                        connectGroupTree.CreatedByPersonAliasId = 1;
                        connectGroupTree.CreatedDateTime = DateTime.Now;

                        //save group
                        var careMinistryContext = new RockContext();
                        careMinistryContext.WrapTransaction( () =>
                        {
                            careMinistryContext.Configuration.AutoDetectChangesEnabled = false;
                            careMinistryContext.Groups.Add( connectGroupTree );
                            careMinistryContext.SaveChanges( DisableAudit );
                        } );
                    }

                    int existingConnectGroup = new GroupService( lookupContext ).Queryable().Where( g => g.Name == groupName ).Select( a => a.Id ).FirstOrDefault();
                    int existingCareMinistries = new GroupService( lookupContext ).Queryable().Where( g => g.Name == "Care Ministries" ).Select( a => a.Id ).FirstOrDefault();
                   
                    //check to see if Connect Group exists.
                    if ( existingConnectGroup == 0 )
                    {
                        //Create one.
                        var careGroup = new Group();
                        careGroup.IsSystem = false;
                        careGroup.GroupTypeId = groupTypeIdSmallGroup;
                        careGroup.ParentGroupId = existingCareMinistries;
                        careGroup.CampusId = 1;
                        careGroup.Name = groupName;
                        careGroup.Description = "";
                        careGroup.IsActive = true;
                        //connectGroups.Order = 0;
                        careGroup.CreatedByPersonAliasId = 1;
                        careGroup.CreatedDateTime = createdDateTime;
                        careGroup.ForeignId = groupId.ToString(); //will use this later for GroupsAttendance

                        //Save Group
                        var careMinistryGroupContext = new RockContext();
                        careMinistryGroupContext.WrapTransaction( () =>
                        {
                            careMinistryGroupContext.Configuration.AutoDetectChangesEnabled = false;
                            careMinistryGroupContext.Groups.Add( careGroup );
                            careMinistryGroupContext.SaveChanges( DisableAudit );
                        } );
                        completedGroups++;
                    }

                    existingConnectGroup = new GroupService( lookupContext ).Queryable().Where( g => g.Name == groupName ).Select( a => a.Id ).FirstOrDefault();

                    //Adds Group Member(s)
                    //makes sure Connect Group Exists
                    if ( existingConnectGroup != 0 )
                    {
                        int memberGroupTypeRoleId = new GroupTypeRoleService( lookupContext ).Queryable().Where( g => g.GroupTypeId == groupTypeIdSmallGroup && g.Name == "Member" ).Select( a => a.Id ).FirstOrDefault();
                        int groupMemberExists = new GroupMemberService( lookupContext ).Queryable().Where( g => g.GroupId == existingConnectGroup && g.PersonId == personId && g.GroupRoleId == memberGroupTypeRoleId ).Select( a => a.Id ).FirstOrDefault();
                        if ( groupMemberExists == 0 )
                        {
                            //adds member
                            var connectGroupMember = new GroupMember();
                            connectGroupMember.IsSystem = false;
                            connectGroupMember.GroupId = existingConnectGroup;
                            connectGroupMember.PersonId = (int)personId;
                            connectGroupMember.GroupRoleId = memberGroupTypeRoleId; //will add them as a member
                            //ReportProgress( 0, string.Format( "GroupId: {0}, GroupName: {3}, PersonID: {1}, GroupRoleId: {2}", connectGroupMember.GroupId, connectGroupMember.PersonId, connectGroupMember.GroupRoleId, groupName ) );

                            //Save Member
                            var careGroupMemberContext = new RockContext();
                            careGroupMemberContext.WrapTransaction( () =>
                            {
                                careGroupMemberContext.Configuration.AutoDetectChangesEnabled = false;
                                careGroupMemberContext.GroupMembers.Add( connectGroupMember );
                                careGroupMemberContext.SaveChanges( DisableAudit );
                            } );
                            completedMembers++;
                        }
                    }

                    if ( completedMembers % percentage < 1 )
                    {
                        int percentComplete = completedMembers / percentage;
                       // ReportProgress( percentComplete, string.Format( "Life Stages Imported: {0}, Groups Imported: {1}, Members Imported: {2} ({3}% complete). ", completedLifeStages, completedGroups, completedMembers, percentComplete ) );
                    }
                    else if ( completedMembers % ReportingNumber < 1 )
                    {
                        ReportPartialProgress();
                    }

                }


                if ( groupTypeName.Trim() == "Intro Connect Groups" )            //Moves Intro Connect Groups into Rock Groups
                {

                    var groupTypeIdSection = new GroupTypeService( lookupContext ).Queryable().Where( gt => gt.Name == "Event/Serving/Small Group Section" ).Select( a => a.Id ).FirstOrDefault();
                    var introConnectGroupsId = new GroupService( lookupContext ).Queryable().Where( g => g.Name == "Intro Connect Groups" && g.GroupTypeId == groupTypeIdSection ).Select( a => a.Id ).FirstOrDefault();
                    var groupTypeIdSmallGroup = new GroupTypeService( lookupContext ).Queryable().Where( gt => gt.Name == "Small Group" ).Select( a => a.Id ).FirstOrDefault();

                    string groupName = row["Group_Name"] as string;
                    int? groupId = row["Group_ID"] as int?;
                    int? individualId = row["Individual_ID"] as int?;
                    int? personId = GetPersonAliasId( individualId );
                    DateTime? createdDateTime = row["Created_Date"] as DateTime?;


                    //Check to see if Head of Care Ministries Tree exists

                    //If it doesn't exist
                    if ( introConnectGroupsId == 0 )
                    {
                        //Create one.
                        var connectGroupTree = new Group();
                        connectGroupTree.IsSystem = false;
                        connectGroupTree.GroupTypeId = groupTypeIdSection;
                        connectGroupTree.CampusId = 1;
                        connectGroupTree.Name = "Intro Connect Groups";
                        connectGroupTree.Description = "Crossroads Intro Connect Groups";
                        connectGroupTree.IsActive = true;
                        //connectGroupTree.Order = 0;
                        connectGroupTree.CreatedByPersonAliasId = 1;
                        connectGroupTree.CreatedDateTime = DateTime.Now;

                        //save group
                        var introConnectGroupTreeContext = new RockContext();
                         introConnectGroupTreeContext.WrapTransaction( () =>
                        {
                            introConnectGroupTreeContext.Configuration.AutoDetectChangesEnabled = false;
                            introConnectGroupTreeContext.Groups.Add( connectGroupTree );
                            introConnectGroupTreeContext.SaveChanges( DisableAudit );
                        } );
                    }

                    int existingConnectGroup = new GroupService( lookupContext ).Queryable().Where( g => g.Name == groupName ).Select( a => a.Id ).FirstOrDefault();
                    int existingIntroConnectGroup = new GroupService( lookupContext ).Queryable().Where( g => g.Name == "Intro Connect Groups" ).Select( a => a.Id ).FirstOrDefault();

                    //check to see if Connect Group exists.
                    if ( existingConnectGroup == 0 )
                    {
                        //Create one.
                        var introConnectGroup = new Group();
                        introConnectGroup.IsSystem = false;
                        introConnectGroup.GroupTypeId = groupTypeIdSmallGroup;
                        introConnectGroup.ParentGroupId = existingIntroConnectGroup;
                        introConnectGroup.CampusId = 1;
                        introConnectGroup.Name = groupName;
                        introConnectGroup.Description = "";
                        introConnectGroup.IsActive = true;
                        //connectGroups.Order = 0;
                        introConnectGroup.CreatedByPersonAliasId = 1;
                        introConnectGroup.CreatedDateTime = createdDateTime;
                        introConnectGroup.ForeignId = groupId.ToString(); //will use this later for GroupsAttendance

                        //Save Group
                        var introConnectGroupConext = new RockContext();
                        introConnectGroupConext.WrapTransaction( () =>
                        {
                            introConnectGroupConext.Configuration.AutoDetectChangesEnabled = false;
                            introConnectGroupConext.Groups.Add( introConnectGroup );
                            introConnectGroupConext.SaveChanges( DisableAudit );
                        } );
                        completedGroups++;
                    }

                    existingConnectGroup = new GroupService( lookupContext ).Queryable().Where( g => g.Name == groupName ).Select( a => a.Id ).FirstOrDefault();

                    //Adds Group Member(s)
                    //makes sure Connect Group Exists
                    if ( existingConnectGroup != 0 )
                    {
                        int memberGroupTypeRoleId = new GroupTypeRoleService( lookupContext ).Queryable().Where( g => g.GroupTypeId == groupTypeIdSmallGroup && g.Name == "Member" ).Select( a => a.Id ).FirstOrDefault();
                        int groupMemberExists = new GroupMemberService( lookupContext ).Queryable().Where( g => g.GroupId == existingConnectGroup && g.PersonId == personId && g.GroupRoleId == memberGroupTypeRoleId ).Select( a => a.Id ).FirstOrDefault();
                        if ( groupMemberExists == 0 )
                        {
                            //adds member
                            var connectGroupMember = new GroupMember();
                            connectGroupMember.IsSystem = false;
                            connectGroupMember.GroupId = existingConnectGroup;
                            connectGroupMember.PersonId = (int)personId;
                            connectGroupMember.GroupRoleId = memberGroupTypeRoleId; //will add them as a member
                            //ReportProgress( 0, string.Format( "GroupId: {0}, GroupName: {3}, PersonID: {1}, GroupRoleId: {2}", connectGroupMember.GroupId, connectGroupMember.PersonId, connectGroupMember.GroupRoleId, groupName ) );

                            //Save Member
                            var introConnectGroupMemberConext = new RockContext();
                            introConnectGroupMemberConext.WrapTransaction( () =>
                            {
                                introConnectGroupMemberConext.Configuration.AutoDetectChangesEnabled = false;
                                introConnectGroupMemberConext.GroupMembers.Add( connectGroupMember );
                                introConnectGroupMemberConext.SaveChanges( DisableAudit );
                            } );
                            completedMembers++;
                        }
                    }

                    if ( completedMembers % percentage < 1 )
                    {
                        int percentComplete = completedMembers / percentage;
                       // ReportProgress( percentComplete, string.Format( "Life Stages Imported: {0}, Groups Imported: {1}, Members Imported: {2} ({3}% complete). ", completedLifeStages, completedGroups, completedMembers, percentComplete ) );
                    }
                    else if ( completedMembers % ReportingNumber < 1 )
                    {
                        ReportPartialProgress();
                    }

                }




                if ( groupTypeName.Trim() == "People List" )    //Places People Lists in tags
                {

                    var tagService = new TagService( lookupContext );
                    var entityTypeService = new EntityTypeService( lookupContext );
                    var taggedItemService = new TaggedItemService( lookupContext );
                    var personService = new PersonService( lookupContext );

                    //var groupTypeIdSection = new GroupTypeService( lookupContext ).Queryable().Where( gt => gt.Name == "Event/Serving/Small Group Section" ).Select( a => a.Id ).FirstOrDefault();
                    //var connectGroupsId = new GroupService( lookupContext ).Queryable().Where( g => g.Name == "Connect Groups" && g.GroupTypeId == groupTypeIdSection ).Select( a => a.Id ).FirstOrDefault();
                    //var groupTypeIdSmallGroup = new GroupTypeService( lookupContext ).Queryable().Where( gt => gt.Name == "Small Group" ).Select( a => a.Id ).FirstOrDefault();


                    string peopleListName = row["Group_Name"] as string;
                    int? groupId = row["Group_ID"] as int?;
                    int? individualId = row["Individual_ID"] as int?;
                    int? personId = GetPersonAliasId( individualId );
                    DateTime? createdDateTime = row["Created_Date"] as DateTime?;

                    if ( personId != null )
                    {
                        //check if tag exists
                        if ( tagService.Queryable().Where( t => t.Name == peopleListName ).FirstOrDefault() == null )
                        {
                            //create if it doesn't
                            var newTag = new Tag();
                            newTag.IsSystem = false;
                            newTag.Name = peopleListName;
                            newTag.EntityTypeQualifierColumn = string.Empty;
                            newTag.EntityTypeQualifierValue = string.Empty;
                            newTag.EntityTypeId = entityTypeService.Queryable().Where( e => e.Name == "Rock.Model.Person" ).FirstOrDefault().Id;

                            //Save tag
                            var tagContext = new RockContext();
                            tagContext.WrapTransaction( () =>
                            {
                                tagContext.Configuration.AutoDetectChangesEnabled = false;
                                tagContext.Tags.Add( newTag );
                                tagContext.SaveChanges( DisableAudit );
                            } );

                            completedTags++;
                        }

                        var personAlias = new PersonAlias();
                        personAlias = null;

                        if ( tagService.Queryable().Where( t => t.Name == peopleListName ).FirstOrDefault() != null ) //Makes sure tag exists
                        {
                            //selects the ID of the current people list / tag
                            int tagId = tagService.Queryable().Where( t => t.Name == peopleListName ).FirstOrDefault().Id;
                            
                            //gets the person instance in order to use person's GUID later.
                            var personTagged = personService.Queryable().Where( p => p.Id == personId ).FirstOrDefault();
                            if ( personTagged == null )
                            {
                                var personAliasService = new PersonAliasService(lookupContext);
                                personAlias = personAliasService.Queryable().Where( p => p.PersonId == (int)personId ).FirstOrDefault();
                                //ReportProgress( 0, string.Format( "Not able to tag person Id: {0} Tag Name: {1} F1 groupId: {2} Tag Id: {3}. ", personId, peopleListName, groupId, tagId ) );

                            }

                            //check if person already has this tag
                            if ( personTagged != null && taggedItemService.Queryable().Where( t => t.EntityGuid == personTagged.Guid && t.TagId == tagId ).FirstOrDefault() == null )
                            {

                                //add tag if one doesn't exist for person.
                                var taggedItem = new TaggedItem();
                                taggedItem.IsSystem = false;
                                taggedItem.TagId = tagId;
                                taggedItem.EntityGuid = personTagged.Guid;
                                taggedItem.CreatedDateTime = createdDateTime;

                                //save tag
                                var tagContext = new RockContext();
                                tagContext.WrapTransaction( () =>
                                {
                                    tagContext.Configuration.AutoDetectChangesEnabled = false;
                                    tagContext.TaggedItems.Add( taggedItem );
                                    tagContext.SaveChanges( DisableAudit );
                                } );

                                completedIndividualTags++;
                            }
                            if ( personAlias != null && taggedItemService.Queryable().Where( t => t.EntityGuid == personAlias.AliasPersonGuid && t.TagId == tagId ).FirstOrDefault() == null )
                            {

                                //add tag if one doesn't exist for person.
                                var taggedItem = new TaggedItem();
                                taggedItem.IsSystem = false;
                                taggedItem.TagId = tagId;
                                taggedItem.EntityGuid = personAlias.AliasPersonGuid;
                                taggedItem.CreatedDateTime = createdDateTime;

                                //save tag
                                var tagContext = new RockContext();
                                tagContext.WrapTransaction( () =>
                                {
                                    tagContext.Configuration.AutoDetectChangesEnabled = false;
                                    tagContext.TaggedItems.Add( taggedItem );
                                    tagContext.SaveChanges( DisableAudit );
                                } );

                                completedIndividualTags++;
                            }
                        }
                        //report Progress
                        if ( completedIndividualTags != 0 )
                        {
                            if ( completedIndividualTags % percentage < 1 )
                            {
                                int percentComplete = completedIndividualTags / percentage;
                               // ReportProgress( percentComplete, string.Format( "People Lists / Tags Imported: {0:N0}, Tagged Individuals: {1:N0} ({2:N0}% complete). ", completedTags, completedIndividualTags, percentComplete ) );
                            }
                            else if ( completedMembers % ReportingNumber < 1 )
                            {
                                ReportPartialProgress();
                            }
                        }
                    }
                }
            }
        }
    }
}
