﻿// <copyright>
// Copyright by the Central Christian Church
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;

using Rock.Model;
using Rock.Data;

namespace com.centralaz.RoomManagement.Model
{
    /// <summary>
    /// A Reservation Location
    /// </summary>
    [Table( "_com_centralaz_RoomManagement_ReservationLocation" )]
    [DataContract]
    public class ReservationLocation : Rock.Data.Model<ReservationLocation>, Rock.Data.IRockEntity
    {

        #region Entity Properties

        [DataMember]
        public int ReservationId { get; set; }

        [DataMember]
        public int LocationId { get; set; }

        [DataMember]
        public ReservationLocationApprovalState ApprovalState { get; set; }

        #endregion

        #region Virtual Properties

        public virtual Reservation Reservation { get; set; }
        
        [LavaInclude]
        public virtual Location Location { get; set; }

        #endregion

        #region Methods

        public void CopyPropertiesFrom( ReservationLocation source )
        {
            this.Id = source.Id;
            this.ForeignGuid = source.ForeignGuid;
            this.ForeignKey = source.ForeignKey;
            this.ReservationId = source.ReservationId;
            this.LocationId = source.LocationId;
            this.ApprovalState = source.ApprovalState;
            this.CreatedDateTime = source.CreatedDateTime;
            this.ModifiedDateTime = source.ModifiedDateTime;
            this.CreatedByPersonAliasId = source.CreatedByPersonAliasId;
            this.ModifiedByPersonAliasId = source.ModifiedByPersonAliasId;
            this.Guid = source.Guid;
            this.ForeignId = source.ForeignId;
        }

        #endregion
    }

    #region Entity Configuration


    public partial class ReservationLocationConfiguration : EntityTypeConfiguration<ReservationLocation>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReservationLocationConfiguration"/> class.
        /// </summary>
        public ReservationLocationConfiguration()
        {
            this.HasRequired( r => r.Reservation ).WithMany( r => r.ReservationLocations ).HasForeignKey( r => r.ReservationId ).WillCascadeOnDelete( true );
            this.HasRequired( r => r.Location ).WithMany().HasForeignKey( r => r.LocationId ).WillCascadeOnDelete( false );

            // IMPORTANT!!
            this.HasEntitySetName( "ReservationLocation" );
        }
    }

    #endregion

    #region Enumerations

    /// <summary>
    /// An enum that represents when a Job notification status should be sent.
    /// </summary>
    public enum ReservationLocationApprovalState
    {
        /// <summary>
        /// Notifications should be sent when a job completes with any notification status.
        /// </summary>
        Unapproved = 1,

        /// <summary>
        /// Notification should be sent when the job has completed successfully.
        /// </summary>
        /// 
        Approved = 2,

        /// <summary>
        /// Notification should be sent when the job has completed with an error status.
        /// </summary>
        Denied = 3
    }

    #endregion
}
