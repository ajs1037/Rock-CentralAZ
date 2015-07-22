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
namespace Rock.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    /// <summary>
    ///
    /// </summary>
    public partial class FollowingEventsSuggestions : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            CreateTable(
                "dbo.FollowingEvent",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                        Description = c.String(),
                        EntityTypeId = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        SendOnWeekends = c.Boolean(nullable: false),
                        LastCheckDateTime = c.DateTime(),
                        IsNoticeRequired = c.Boolean(nullable: false),
                        CreatedDateTime = c.DateTime(),
                        ModifiedDateTime = c.DateTime(),
                        CreatedByPersonAliasId = c.Int(),
                        ModifiedByPersonAliasId = c.Int(),
                        Guid = c.Guid(nullable: false),
                        ForeignId = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("dbo.EntityType", t => t.EntityTypeId)
                .ForeignKey("dbo.PersonAlias", t => t.ModifiedByPersonAliasId)
                .Index(t => t.EntityTypeId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true)
                .Index(t => t.ForeignId);
            
            CreateTable(
                "dbo.FollowingSuggestion",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                        Description = c.String(),
                        EntityTypeId = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        CreatedDateTime = c.DateTime(),
                        ModifiedDateTime = c.DateTime(),
                        CreatedByPersonAliasId = c.Int(),
                        ModifiedByPersonAliasId = c.Int(),
                        Guid = c.Guid(nullable: false),
                        ForeignId = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("dbo.EntityType", t => t.EntityTypeId)
                .ForeignKey("dbo.PersonAlias", t => t.ModifiedByPersonAliasId)
                .Index(t => t.EntityTypeId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true)
                .Index(t => t.ForeignId);

            // Following component types
            RockMigrationHelper.UpdateEntityType( "Rock.Model.FollowingEvent", "8A0D208B-762D-403A-A972-3A0F079866D4", true, true );
            RockMigrationHelper.UpdateEntityType( "Rock.Model.FollowingSuggestion", "CC7DF118-86A1-4F90-82D8-0DAE9CD37343", true, true );

            // Birthday event
            RockMigrationHelper.UpdateEntityType( "Rock.Follow.PersonBirthdayEvent", "532A7405-A3FB-4147-BE67-3B75A230AADE", false, true );
            RockMigrationHelper.UpdateEntityAttribute( "Rock.Model.FollowingEvent", "Rock.Follow.PersonBirthdayEvent", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Lead Days",
                "The number of days prior to birthday that notification should be sent.", 0, "5", "F5B35909-5A4A-4203-84A8-7F493E56548B" );
            Sql( @"
    DECLARE @EntityTypeId int = ( SELECT TOP 1 [Id] FROM [EntityType] WHERE [Guid] = '532A7405-A3FB-4147-BE67-3B75A230AADE' )
    DECLARE @AttributeId int = ( SELECT TOP 1 [Id] FROM [Attribute] WHERE [Guid] = 'F5B35909-5A4A-4203-84A8-7F493E56548B' )
    IF @EntityTypeId IS NOT NULL
    BEGIN
	    INSERT INTO [FollowingEvent] ( [Name], [Description], [EntityTypeId], [IsActive], [SendOnWeekends], [IsNoticeRequired], [Guid] )
	    VALUES 
	      ( 'Person Birthday (5 day notice)', 'Five day notice of an upcoming birthday.', @EntityTypeId, 1, 0, 0, 'E1C2F8BD-E875-4C7B-91A1-EDB98AB01BDC' ),
	      ( 'Person Birthday (day of notice)', 'Notice of a birthday today.', @EntityTypeId, 1, 0, 0, 'F3A577DB-8F4A-4245-BD00-0B2B8F789131' )
	    IF @AttributeId IS NOT NULL
	    BEGIN
		    INSERT INTO [AttributeValue] ( [IsSystem], [AttributeId], [EntityId], [Value], [Guid] )
		    SELECT 0, @AttributeId, [Id], '5', NEWID()
		    FROM [FollowingEvent] WHERE [Guid] = 'E1C2F8BD-E875-4C7B-91A1-EDB98AB01BDC'

		    INSERT INTO [AttributeValue] ( [IsSystem], [AttributeId], [EntityId], [Value], [Guid] )
		    SELECT 0, @AttributeId, [Id], '0', NEWID()
		    FROM [FollowingEvent] WHERE [Guid] = 'F3A577DB-8F4A-4245-BD00-0B2B8F789131'
	    END
    END
" );

            RockMigrationHelper.AddPage( "C831428A-6ACD-4D49-9B2D-046D399E3123", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Following Events", "", "41D9EE79-120E-4359-88A4-74AC87592F50", "" ); // Site:Rock RMS
            RockMigrationHelper.AddPage( "41D9EE79-120E-4359-88A4-74AC87592F50", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Following Event", "", "C9DBB85E-674E-4AC3-85D9-7C7B1C52AFE2", "" ); // Site:Rock RMS
            RockMigrationHelper.UpdateBlockType( "Event List", "Block for viewing list of following events.", "~/Blocks/Follow/EventList.ascx", "Follow", "9021BC5D-F759-4A20-9B01-5D2FBE23AF3E" );
            RockMigrationHelper.UpdateBlockType( "Event Detail", "Displays the details of the given financial event.", "~/Blocks/Follow/EventDetail.ascx", "Follow", "BA77458A-FEE8-4172-8596-37A58D468141" );

            // Add Block to Page: Following Events, Site: Rock RMS
            RockMigrationHelper.AddBlock( "41D9EE79-120E-4359-88A4-74AC87592F50", "", "9021BC5D-F759-4A20-9B01-5D2FBE23AF3E", "Event List", "Main", "", "", 0, "54A98BE8-1D90-4638-B74D-4DA91528F2BC" );
            // Add Block to Page: Following Event, Site: Rock RMS
            RockMigrationHelper.AddBlock( "C9DBB85E-674E-4AC3-85D9-7C7B1C52AFE2", "", "BA77458A-FEE8-4172-8596-37A58D468141", "Event Detail", "Main", "", "", 0, "97AA9D7A-052A-4713-9BAC-B362C0733753" );
            // Attrib for BlockType: Event List:Detail Page
            RockMigrationHelper.AddBlockTypeAttribute( "9021BC5D-F759-4A20-9B01-5D2FBE23AF3E", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Detail Page", "DetailPage", "", "", 0, @"", "8A66F498-FF0E-4627-AB4E-EF94A82F7187" );
            // Attrib Value for Block:Event List, Attribute:Detail Page Page: Following Events, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "54A98BE8-1D90-4638-B74D-4DA91528F2BC", "8A66F498-FF0E-4627-AB4E-EF94A82F7187", @"c9dbb85e-674e-4ac3-85d9-7c7b1c52afe2" );

        }
        
        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            DropForeignKey("dbo.FollowingSuggestion", "ModifiedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo.FollowingSuggestion", "EntityTypeId", "dbo.EntityType");
            DropForeignKey("dbo.FollowingSuggestion", "CreatedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo.FollowingEvent", "ModifiedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo.FollowingEvent", "EntityTypeId", "dbo.EntityType");
            DropForeignKey("dbo.FollowingEvent", "CreatedByPersonAliasId", "dbo.PersonAlias");
            DropIndex("dbo.FollowingSuggestion", new[] { "ForeignId" });
            DropIndex("dbo.FollowingSuggestion", new[] { "Guid" });
            DropIndex("dbo.FollowingSuggestion", new[] { "ModifiedByPersonAliasId" });
            DropIndex("dbo.FollowingSuggestion", new[] { "CreatedByPersonAliasId" });
            DropIndex("dbo.FollowingSuggestion", new[] { "EntityTypeId" });
            DropIndex("dbo.FollowingEvent", new[] { "ForeignId" });
            DropIndex("dbo.FollowingEvent", new[] { "Guid" });
            DropIndex("dbo.FollowingEvent", new[] { "ModifiedByPersonAliasId" });
            DropIndex("dbo.FollowingEvent", new[] { "CreatedByPersonAliasId" });
            DropIndex("dbo.FollowingEvent", new[] { "EntityTypeId" });
            DropTable("dbo.FollowingSuggestion");
            DropTable("dbo.FollowingEvent");
        }
    }
}
