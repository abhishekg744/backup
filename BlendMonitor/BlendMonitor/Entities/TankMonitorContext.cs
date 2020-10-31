using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BlendMonitor.Entities
{
    public partial class BlendMonitorContext : DbContext
    {
        public BlendMonitorContext()
        {
        }

        public BlendMonitorContext(DbContextOptions<BlendMonitorContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AbcAnzHdrProps> AbcAnzHdrProps { get; set; }
        public virtual DbSet<AbcAnzs> AbcAnzs { get; set; }
        public virtual DbSet<AbcAnzsStates> AbcAnzsStates { get; set; }
        public virtual DbSet<AbcBlendCompProps> AbcBlendCompProps { get; set; }
        public virtual DbSet<AbcBlendComps> AbcBlendComps { get; set; }
        public virtual DbSet<AbcBlendDest> AbcBlendDest { get; set; }
        public virtual DbSet<AbcBlendDestProps> AbcBlendDestProps { get; set; }
        public virtual DbSet<AbcBlendDestSeq> AbcBlendDestSeq { get; set; }
        public virtual DbSet<AbcBlendIntervalComps> AbcBlendIntervalComps { get; set; }
        public virtual DbSet<AbcBlendIntervalProps> AbcBlendIntervalProps { get; set; }
        public virtual DbSet<AbcBlendIntervals> AbcBlendIntervals { get; set; }
        public virtual DbSet<AbcBlendProps> AbcBlendProps { get; set; }
        public virtual DbSet<AbcBlendSampleProps> AbcBlendSampleProps { get; set; }
        public virtual DbSet<AbcBlendSamples> AbcBlendSamples { get; set; }
        public virtual DbSet<AbcBlendSources> AbcBlendSources { get; set; }
        public virtual DbSet<AbcBlendStations> AbcBlendStations { get; set; }
        public virtual DbSet<AbcBlendSwings> AbcBlendSwings { get; set; }
        public virtual DbSet<AbcBlenderComps> AbcBlenderComps { get; set; }
        public virtual DbSet<AbcBlenderDest> AbcBlenderDest { get; set; }
        public virtual DbSet<AbcBlenderSources> AbcBlenderSources { get; set; }
        public virtual DbSet<AbcBlenders> AbcBlenders { get; set; }
        public virtual DbSet<AbcBlends> AbcBlends { get; set; }
        public virtual DbSet<AbcCalcCoefficients> AbcCalcCoefficients { get; set; }
        public virtual DbSet<AbcCalcRoutines> AbcCalcRoutines { get; set; }
        public virtual DbSet<AbcCompLineupEqp> AbcCompLineupEqp { get; set; }
        public virtual DbSet<AbcCompLineups> AbcCompLineups { get; set; }
        public virtual DbSet<AbcGrades> AbcGrades { get; set; }
        public virtual DbSet<AbcIcons> AbcIcons { get; set; }
        public virtual DbSet<AbcLabTankData> AbcLabTankData { get; set; }
        public virtual DbSet<AbcLineupGeo> AbcLineupGeo { get; set; }
        public virtual DbSet<AbcMaterials> AbcMaterials { get; set; }
        public virtual DbSet<AbcPrdAdditives> AbcPrdAdditives { get; set; }
        public virtual DbSet<AbcPrdPropSpecs> AbcPrdPropSpecs { get; set; }
        public virtual DbSet<AbcPrdgrpMatProps> AbcPrdgrpMatProps { get; set; }
        public virtual DbSet<AbcPrdgrpProps> AbcPrdgrpProps { get; set; }
        public virtual DbSet<AbcPrdgrpUsages> AbcPrdgrpUsages { get; set; }
        public virtual DbSet<AbcPrdgrps> AbcPrdgrps { get; set; }
        public virtual DbSet<AbcProdLineupEqp> AbcProdLineupEqp { get; set; }
        public virtual DbSet<AbcProdLineups> AbcProdLineups { get; set; }
        public virtual DbSet<AbcPrograms> AbcPrograms { get; set; }
        public virtual DbSet<AbcProjDefaults> AbcProjDefaults { get; set; }
        public virtual DbSet<AbcPropSources> AbcPropSources { get; set; }
        public virtual DbSet<AbcProperties> AbcProperties { get; set; }
        public virtual DbSet<AbcPumps> AbcPumps { get; set; }
        public virtual DbSet<AbcRbcStates> AbcRbcStates { get; set; }
        public virtual DbSet<AbcScanGroups> AbcScanGroups { get; set; }
        public virtual DbSet<AbcStations> AbcStations { get; set; }
        public virtual DbSet<AbcSwingCriteria> AbcSwingCriteria { get; set; }
        public virtual DbSet<AbcSwingStates> AbcSwingStates { get; set; }
        public virtual DbSet<AbcTags> AbcTags { get; set; }
        public virtual DbSet<AbcTankComposition> AbcTankComposition { get; set; }
        public virtual DbSet<AbcTankProps> AbcTankProps { get; set; }
        public virtual DbSet<AbcTankStates> AbcTankStates { get; set; }
        public virtual DbSet<AbcTanks> AbcTanks { get; set; }
        public virtual DbSet<AbcTranstxt> AbcTranstxt { get; set; }
        public virtual DbSet<AbcUnitConversion> AbcUnitConversion { get; set; }
        public virtual DbSet<AbcUom> AbcUom { get; set; }
        public virtual DbSet<AbcUsages> AbcUsages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlServer("Server=DESKTOP-1IC6D7F\\SQLEXPRESS;Database=ABCTest3;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AbcAnzHdrProps>(entity =>
            {
                entity.HasKey(e => new { e.BlenderId, e.PropId, e.AnzId })
                    .HasName("PK_ANZ_HPROPS");

                entity.ToTable("ABC_ANZ_HDR_PROPS");

                entity.HasIndex(e => e.ResTagId)
                    .HasName("UQ_RES_TAG_ID_ANZ_HPROPS")
                    .IsUnique();

                entity.Property(e => e.BlenderId).HasColumnName("BLENDER_ID");

                entity.Property(e => e.PropId).HasColumnName("PROP_ID");

                entity.Property(e => e.AnzId).HasColumnName("ANZ_ID");

                entity.Property(e => e.BiasFilter).HasColumnName("BIAS_FILTER");

                entity.Property(e => e.CalibrationAccuracy)
                    .HasColumnName("CALIBRATION_ACCURACY")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.CurOp).HasColumnName("CUR_OP");

                entity.Property(e => e.CurRes).HasColumnName("CUR_RES");

                entity.Property(e => e.CurStatus)
                    .HasColumnName("CUR_STATUS")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO BLEND')");

                entity.Property(e => e.FilterFactor).HasColumnName("FILTER_FACTOR");

                entity.Property(e => e.FrozenOpLmt).HasColumnName("FROZEN_OP_LMT");

                entity.Property(e => e.GoodFlag)
                    .IsRequired()
                    .HasColumnName("GOOD_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.InUseFlag)
                    .HasColumnName("IN_USE_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LowerLmt).HasColumnName("LOWER_LMT");

                entity.Property(e => e.ModelErr).HasColumnName("MODEL_ERR");

                entity.Property(e => e.ModelErrThrsh).HasColumnName("MODEL_ERR_THRSH");

                entity.Property(e => e.NoiseLevel).HasColumnName("NOISE_LEVEL");

                entity.Property(e => e.Offset).HasColumnName("OFFSET");

                entity.Property(e => e.OffsetTime)
                    .HasColumnName("OFFSET_TIME")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.PrevRes).HasColumnName("PREV_RES");

                entity.Property(e => e.RateLmt).HasColumnName("RATE_LMT");

                entity.Property(e => e.ResAvailId).HasColumnName("RES_AVAIL_ID");

                entity.Property(e => e.ResStatusId).HasColumnName("RES_STATUS_ID");

                entity.Property(e => e.ResTagId).HasColumnName("RES_TAG_ID");

                entity.Property(e => e.ResTime)
                    .HasColumnName("RES_TIME")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.ResultTimeLimit).HasColumnName("RESULT_TIME_LIMIT");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.RstResAvailId).HasColumnName("RST_RES_AVAIL_ID");

                entity.Property(e => e.TransportTime).HasColumnName("TRANSPORT_TIME");

                entity.Property(e => e.UpperLmt).HasColumnName("UPPER_LMT");

                entity.HasOne(d => d.Anz)
                    .WithMany(p => p.AbcAnzHdrProps)
                    .HasForeignKey(d => d.AnzId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ANZ_ID_HPROPS");

                entity.HasOne(d => d.Blender)
                    .WithMany(p => p.AbcAnzHdrProps)
                    .HasForeignKey(d => d.BlenderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BLENDER_ID_HPROPS");

                entity.HasOne(d => d.Prop)
                    .WithMany(p => p.AbcAnzHdrProps)
                    .HasForeignKey(d => d.PropId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PROP_ID_ANZ_HPROPS");

                entity.HasOne(d => d.ResAvail)
                    .WithMany(p => p.AbcAnzHdrPropsResAvail)
                    .HasForeignKey(d => d.ResAvailId)
                    .HasConstraintName("FK_RES_AVAIL_ID_ANZ_HPROPS");

                entity.HasOne(d => d.ResStatus)
                    .WithMany(p => p.AbcAnzHdrPropsResStatus)
                    .HasForeignKey(d => d.ResStatusId)
                    .HasConstraintName("FK_RES_STATUS_ID_ANZ_HPROPS");

                entity.HasOne(d => d.ResTag)
                    .WithOne(p => p.AbcAnzHdrPropsResTag)
                    .HasForeignKey<AbcAnzHdrProps>(d => d.ResTagId)
                    .HasConstraintName("FK_RES_TAG_ID_HPROPS");

                entity.HasOne(d => d.RstResAvail)
                    .WithMany(p => p.AbcAnzHdrPropsRstResAvail)
                    .HasForeignKey(d => d.RstResAvailId)
                    .HasConstraintName("FK_RST_RES_AVAIL_ID_ANZ_HPROPS");
            });

            modelBuilder.Entity<AbcAnzs>(entity =>
            {
                entity.ToTable("ABC_ANZS");

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_ANZS")
                    .IsUnique();

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.AbcServiceFlag)
                    .IsRequired()
                    .HasColumnName("ABC_SERVICE_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.CalibrateTimeLimit).HasColumnName("CALIBRATE_TIME_LIMIT");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.CycleTime).HasColumnName("CYCLE_TIME");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PrdgrpId).HasColumnName("PRDGRP_ID");

                entity.Property(e => e.ResetRequestFlag)
                    .IsRequired()
                    .HasColumnName("RESET_REQUEST_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.ResultTimeLimit).HasColumnName("RESULT_TIME_LIMIT");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.State)
                    .HasColumnName("STATE")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.StateTagId).HasColumnName("STATE_TAG_ID");

                entity.Property(e => e.TransportTime).HasColumnName("TRANSPORT_TIME");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasColumnName("TYPE")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('CONTINUOUS')");

                entity.HasOne(d => d.StateTag)
                    .WithMany(p => p.AbcAnzs)
                    .HasForeignKey(d => d.StateTagId)
                    .HasConstraintName("FK_STATE_TAGID_ANZS");
            });

            modelBuilder.Entity<AbcAnzsStates>(entity =>
            {
                entity.HasKey(e => e.State)
                    .HasName("PK_ANZS_STATES");

                entity.ToTable("ABC_ANZS_STATES");

                entity.HasIndex(e => e.Alias)
                    .HasName("UQ_ALIAS_ANZS_STATES")
                    .IsUnique();

                entity.HasIndex(e => e.Value)
                    .HasName("UQ_VALUE_ANZS_STATES")
                    .IsUnique();

                entity.Property(e => e.State)
                    .HasColumnName("STATE")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Alias)
                    .IsRequired()
                    .HasColumnName("ALIAS")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.Value).HasColumnName("VALUE");
            });

            modelBuilder.Entity<AbcBlendCompProps>(entity =>
            {
                entity.HasKey(e => new { e.BlendId, e.MatId, e.TankId, e.PropId })
                    .HasName("PK_BLEND_COMP_PROPS");

                entity.ToTable("ABC_BLEND_COMP_PROPS");

                entity.Property(e => e.BlendId).HasColumnName("BLEND_ID");

                entity.Property(e => e.MatId).HasColumnName("MAT_ID");

                entity.Property(e => e.TankId).HasColumnName("TANK_ID");

                entity.Property(e => e.PropId).HasColumnName("PROP_ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.GoodFlag)
                    .IsRequired()
                    .HasColumnName("GOOD_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.RecCreatedTime)
                    .HasColumnName("REC_CREATED_TIME")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.Source)
                    .HasColumnName("SOURCE")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.ValidMax).HasColumnName("VALID_MAX");

                entity.Property(e => e.ValidMin).HasColumnName("VALID_MIN");

                entity.Property(e => e.Value).HasColumnName("VALUE");

                entity.Property(e => e.ValueTime)
                    .HasColumnName("VALUE_TIME")
                    .HasColumnType("datetime2(0)");

                entity.HasOne(d => d.AbcBlendSources)
                    .WithMany(p => p.AbcBlendCompProps)
                    .HasForeignKey(d => new { d.BlendId, d.MatId, d.TankId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BL_MAT_TNK_BLEND_COMP_PROPS");
            });

            modelBuilder.Entity<AbcBlendComps>(entity =>
            {
                entity.HasKey(e => new { e.BlendId, e.MatId })
                    .HasName("PK_BLEND_COMP");

                entity.ToTable("ABC_BLEND_COMPS");

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.BlendId).HasColumnName("BLEND_ID");

                entity.Property(e => e.MatId).HasColumnName("MAT_ID");

                entity.Property(e => e.ActRecipe).HasColumnName("ACT_RECIPE");

                entity.Property(e => e.AvgRecipe).HasColumnName("AVG_RECIPE");

                entity.Property(e => e.Cost).HasColumnName("COST");

                entity.Property(e => e.CostUomId).HasColumnName("COST_UOM_ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.CurRecipe).HasColumnName("CUR_RECIPE");

                entity.Property(e => e.FlowUomId).HasColumnName("FLOW_UOM_ID");

                entity.Property(e => e.HighCons).HasColumnName("HIGH_CONS");

                entity.Property(e => e.HighTarget).HasColumnName("HIGH_TARGET");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LowCons).HasColumnName("LOW_CONS");

                entity.Property(e => e.LowTarget).HasColumnName("LOW_TARGET");

                entity.Property(e => e.MaxFlow).HasColumnName("MAX_FLOW");

                entity.Property(e => e.MinFlow).HasColumnName("MIN_FLOW");

                entity.Property(e => e.NegDevCost).HasColumnName("NEG_DEV_COST");

                entity.Property(e => e.OptRecipe).HasColumnName("OPT_RECIPE");

                entity.Property(e => e.PacingFactor).HasColumnName("PACING_FACTOR");

                entity.Property(e => e.PlanRecipe).HasColumnName("PLAN_RECIPE");

                entity.Property(e => e.PosDevCost).HasColumnName("POS_DEV_COST");

                entity.Property(e => e.PrefRecipe).HasColumnName("PREF_RECIPE");

                entity.Property(e => e.RcpConstraintType)
                    .HasColumnName("RCP_CONSTRAINT_TYPE")
                    .HasMaxLength(12)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('MIN_MAX')");

                entity.Property(e => e.ReqFlowRate).HasColumnName("REQ_FLOW_RATE");

                entity.Property(e => e.RequiredVolume).HasColumnName("REQUIRED_VOLUME");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.TankMax).HasColumnName("TANK_MAX");

                entity.Property(e => e.TankMin).HasColumnName("TANK_MIN");

                entity.Property(e => e.UsageId).HasColumnName("USAGE_ID");

                entity.Property(e => e.VolOffset).HasColumnName("VOL_OFFSET");

                entity.Property(e => e.Volume).HasColumnName("VOLUME");

                entity.Property(e => e.Wild).HasColumnName("WILD");

                entity.HasOne(d => d.Blend)
                    .WithMany(p => p.AbcBlendComps)
                    .HasForeignKey(d => d.BlendId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BLEND_ID_BLEND_COMP");

                entity.HasOne(d => d.CostUom)
                    .WithMany(p => p.AbcBlendCompsCostUom)
                    .HasForeignKey(d => d.CostUomId)
                    .HasConstraintName("FK_CUOM_ID_BLEND_COMP");

                entity.HasOne(d => d.FlowUom)
                    .WithMany(p => p.AbcBlendCompsFlowUom)
                    .HasForeignKey(d => d.FlowUomId)
                    .HasConstraintName("FK_FUOM_ID_BLEND_COMP");

                entity.HasOne(d => d.Mat)
                    .WithMany(p => p.AbcBlendComps)
                    .HasForeignKey(d => d.MatId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MAT_ID_BLEND_COMP");

                entity.HasOne(d => d.Usage)
                    .WithMany(p => p.AbcBlendComps)
                    .HasForeignKey(d => d.UsageId)
                    .HasConstraintName("FK_USAGE_ID_BLEND_COMP");
            });

            modelBuilder.Entity<AbcBlendDest>(entity =>
            {
                entity.HasKey(e => new { e.BlendId, e.TankId })
                    .HasName("PK_BLEND_DEST");

                entity.ToTable("ABC_BLEND_DEST");

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.BlendId).HasColumnName("BLEND_ID");

                entity.Property(e => e.TankId).HasColumnName("TANK_ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.DestSelectName)
                    .HasColumnName("DEST_SELECT_NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.EndLinefillTkFlag)
                    .IsRequired()
                    .HasColumnName("END_LINEFILL_TK_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.FixHeelFlag)
                    .IsRequired()
                    .HasColumnName("FIX_HEEL_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.Property(e => e.FlushTkFlag)
                    .IsRequired()
                    .HasColumnName("FLUSH_TK_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.HeelVolume).HasColumnName("HEEL_VOLUME");

                entity.Property(e => e.InUseFlag)
                    .IsRequired()
                    .HasColumnName("IN_USE_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LineupId).HasColumnName("LINEUP_ID");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.HasOne(d => d.Blend)
                    .WithMany(p => p.AbcBlendDest)
                    .HasForeignKey(d => d.BlendId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BLEND_ID_BLEND_DEST");

                entity.HasOne(d => d.Lineup)
                    .WithMany(p => p.AbcBlendDest)
                    .HasForeignKey(d => d.LineupId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LINEUP_ID_BLEND_DEST");

                entity.HasOne(d => d.Tank)
                    .WithMany(p => p.AbcBlendDest)
                    .HasForeignKey(d => d.TankId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TANK_ID_BLEND_DEST");
            });

            modelBuilder.Entity<AbcBlendDestProps>(entity =>
            {
                entity.HasKey(e => new { e.BlendId, e.TankId, e.PropId })
                    .HasName("PK_BLEND_DEST_PROPS");

                entity.ToTable("ABC_BLEND_DEST_PROPS");

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.BlendId).HasColumnName("BLEND_ID");

                entity.Property(e => e.TankId).HasColumnName("TANK_ID");

                entity.Property(e => e.PropId).HasColumnName("PROP_ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.CurrentValue).HasColumnName("CURRENT_VALUE");

                entity.Property(e => e.HeelValue).HasColumnName("HEEL_VALUE");

                entity.Property(e => e.LabTime)
                    .HasColumnName("LAB_TIME")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LabValue).HasColumnName("LAB_VALUE");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.OnSpecFlag)
                    .IsRequired()
                    .HasColumnName("ON_SPEC_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.HasOne(d => d.AbcBlendDest)
                    .WithMany(p => p.AbcBlendDestProps)
                    .HasForeignKey(d => new { d.BlendId, d.TankId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BL_TNK_BLEND_DEST_PROPS");
            });

            modelBuilder.Entity<AbcBlendDestSeq>(entity =>
            {
                entity.HasKey(e => new { e.BlendId, e.TankId, e.SwingSequence })
                    .HasName("PK_BLEND_DEST_SEQ");

                entity.ToTable("ABC_BLEND_DEST_SEQ");

                entity.Property(e => e.BlendId).HasColumnName("BLEND_ID");

                entity.Property(e => e.TankId).HasColumnName("TANK_ID");

                entity.Property(e => e.SwingSequence).HasColumnName("SWING_SEQUENCE");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.TimeIn)
                    .HasColumnName("TIME_IN")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.TimeOut)
                    .HasColumnName("TIME_OUT")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.VolAdded).HasColumnName("VOL_ADDED");

                entity.HasOne(d => d.AbcBlendDest)
                    .WithMany(p => p.AbcBlendDestSeq)
                    .HasForeignKey(d => new { d.BlendId, d.TankId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BLEND_TANK_BLEND_DEST_SEQ");
            });

            modelBuilder.Entity<AbcBlendIntervalComps>(entity =>
            {
                entity.HasKey(e => new { e.BlendId, e.MatId, e.Sequence })
                    .HasName("PK_BLEND_INT_COMP");

                entity.ToTable("ABC_BLEND_INTERVAL_COMPS");

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.BlendId).HasColumnName("BLEND_ID");

                entity.Property(e => e.MatId).HasColumnName("MAT_ID");

                entity.Property(e => e.Sequence).HasColumnName("SEQUENCE");

                entity.Property(e => e.AvgHighTarget).HasColumnName("AVG_HIGH_TARGET");

                entity.Property(e => e.AvgLowTarget).HasColumnName("AVG_LOW_TARGET");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.HighTarget).HasColumnName("HIGH_TARGET");

                entity.Property(e => e.IntRecipe).HasColumnName("INT_RECIPE");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LowTarget).HasColumnName("LOW_TARGET");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SpRecipe).HasColumnName("SP_RECIPE");

                entity.Property(e => e.Volume).HasColumnName("VOLUME");

                entity.HasOne(d => d.Mat)
                    .WithMany(p => p.AbcBlendIntervalComps)
                    .HasForeignKey(d => d.MatId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MAT_ID_BLEND_INTERVAL_COMP");

                entity.HasOne(d => d.AbcBlendIntervals)
                    .WithMany(p => p.AbcBlendIntervalComps)
                    .HasForeignKey(d => new { d.BlendId, d.Sequence })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BL_SEQ_BLEND_INTERVAL_COMP");
            });

            modelBuilder.Entity<AbcBlendIntervalProps>(entity =>
            {
                entity.HasKey(e => new { e.BlendId, e.PropId, e.Sequence })
                    .HasName("PK_BLEND_INT_PROP");

                entity.ToTable("ABC_BLEND_INTERVAL_PROPS");

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.BlendId).HasColumnName("BLEND_ID");

                entity.Property(e => e.PropId).HasColumnName("PROP_ID");

                entity.Property(e => e.Sequence).HasColumnName("SEQUENCE");

                entity.Property(e => e.AnzGoodFlag)
                    .IsRequired()
                    .HasColumnName("ANZ_GOOD_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.AnzId).HasColumnName("ANZ_ID");

                entity.Property(e => e.AnzRes).HasColumnName("ANZ_RES");

                entity.Property(e => e.Bias).HasColumnName("BIAS");

                entity.Property(e => e.BiascalcCurrent)
                    .HasColumnName("BIASCALC_CURRENT")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.CalcPropertyFlag)
                    .IsRequired()
                    .HasColumnName("CALC_PROPERTY_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.DestPred).HasColumnName("DEST_PRED");

                entity.Property(e => e.FbPredBias).HasColumnName("FB_PRED_BIAS");

                entity.Property(e => e.FeedbackPred).HasColumnName("FEEDBACK_PRED");

                entity.Property(e => e.HeaderMax).HasColumnName("HEADER_MAX");

                entity.Property(e => e.HeaderMin).HasColumnName("HEADER_MIN");

                entity.Property(e => e.HighTarget).HasColumnName("HIGH_TARGET");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LowTarget).HasColumnName("LOW_TARGET");

                entity.Property(e => e.ResultCnt).HasColumnName("RESULT_CNT");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SetpointPred).HasColumnName("SETPOINT_PRED");

                entity.Property(e => e.UnfiltBias).HasColumnName("UNFILT_BIAS");

                entity.HasOne(d => d.Anz)
                    .WithMany(p => p.AbcBlendIntervalProps)
                    .HasForeignKey(d => d.AnzId)
                    .HasConstraintName("FK_ANZ_ID_BLEND_INTERVAL_PROP");

                entity.HasOne(d => d.Prop)
                    .WithMany(p => p.AbcBlendIntervalProps)
                    .HasForeignKey(d => d.PropId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PROP_ID_BLEND_INTERVAL_PROP");

                entity.HasOne(d => d.AbcBlendIntervals)
                    .WithMany(p => p.AbcBlendIntervalProps)
                    .HasForeignKey(d => new { d.BlendId, d.Sequence })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BL_SEQ_BLEND_INTERVAL_PROP");
            });

            modelBuilder.Entity<AbcBlendIntervals>(entity =>
            {
                entity.HasKey(e => new { e.BlendId, e.Sequence })
                    .HasName("PK_BLEND_INTERVALS");

                entity.ToTable("ABC_BLEND_INTERVALS");

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.BlendId).HasColumnName("BLEND_ID");

                entity.Property(e => e.Sequence).HasColumnName("SEQUENCE");

                entity.Property(e => e.BlendVolume).HasColumnName("BLEND_VOLUME");

                entity.Property(e => e.Cost).HasColumnName("COST");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.OptRunId).HasColumnName("OPT_RUN_ID");

                entity.Property(e => e.OptimizerSetting).HasColumnName("OPTIMIZER_SETTING");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.Starttime)
                    .HasColumnName("STARTTIME")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Stoptime)
                    .HasColumnName("STOPTIME")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.UnfiltBias).HasColumnName("UNFILT_BIAS");

                entity.Property(e => e.Volume).HasColumnName("VOLUME");

                entity.HasOne(d => d.Blend)
                    .WithMany(p => p.AbcBlendIntervals)
                    .HasForeignKey(d => d.BlendId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BLEND_ID_BLEND_INTERVALS");
            });

            modelBuilder.Entity<AbcBlendProps>(entity =>
            {
                entity.HasKey(e => new { e.BlendId, e.PropId })
                    .HasName("PK_BLEND_PROP");

                entity.ToTable("ABC_BLEND_PROPS");

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.BlendId).HasColumnName("BLEND_ID");

                entity.Property(e => e.PropId).HasColumnName("PROP_ID");

                entity.Property(e => e.AnalysisMethod)
                    .HasColumnName("ANALYSIS_METHOD")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.AnzOffset).HasColumnName("ANZ_OFFSET");

                entity.Property(e => e.AnzResTagId).HasColumnName("ANZ_RES_TAG_ID");

                entity.Property(e => e.CalcId).HasColumnName("CALC_ID");

                entity.Property(e => e.ControlMax).HasColumnName("CONTROL_MAX");

                entity.Property(e => e.ControlMin).HasColumnName("CONTROL_MIN");

                entity.Property(e => e.Controlled)
                    .IsRequired()
                    .HasColumnName("CONTROLLED")
                    .HasMaxLength(5)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.CorrectionFactor).HasColumnName("CORRECTION_FACTOR");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Giveawaycost).HasColumnName("GIVEAWAYCOST");

                entity.Property(e => e.HdrMax).HasColumnName("HDR_MAX");

                entity.Property(e => e.HdrMin).HasColumnName("HDR_MIN");

                entity.Property(e => e.HlimMax).HasColumnName("HLIM_MAX");

                entity.Property(e => e.HlimMin).HasColumnName("HLIM_MIN");

                entity.Property(e => e.InitialBias).HasColumnName("INITIAL_BIAS");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.ModelErrClrdFlag)
                    .IsRequired()
                    .HasColumnName("MODEL_ERR_CLRD_FLAG")
                    .HasMaxLength(5)
                    .IsUnicode(false);

                entity.Property(e => e.ModelErrExistsFlag)
                    .IsRequired()
                    .HasColumnName("MODEL_ERR_EXISTS_FLAG")
                    .HasMaxLength(5)
                    .IsUnicode(false);

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SalesMax).HasColumnName("SALES_MAX");

                entity.Property(e => e.SalesMin).HasColumnName("SALES_MIN");

                entity.Property(e => e.SourceId).HasColumnName("SOURCE_ID");

                entity.Property(e => e.ValidMax).HasColumnName("VALID_MAX");

                entity.Property(e => e.ValidMin).HasColumnName("VALID_MIN");

                entity.Property(e => e.Value).HasColumnName("VALUE");

                entity.HasOne(d => d.Blend)
                    .WithMany(p => p.AbcBlendProps)
                    .HasForeignKey(d => d.BlendId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BLEND_ID_BLEND_PROP");

                entity.HasOne(d => d.Calc)
                    .WithMany(p => p.AbcBlendProps)
                    .HasForeignKey(d => d.CalcId)
                    .HasConstraintName("FK_CALC_ID_BLEND_PROPS");

                entity.HasOne(d => d.Prop)
                    .WithMany(p => p.AbcBlendProps)
                    .HasForeignKey(d => d.PropId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PROP_ID_BLEND_PROP");

                entity.HasOne(d => d.Source)
                    .WithMany(p => p.AbcBlendProps)
                    .HasForeignKey(d => d.SourceId)
                    .HasConstraintName("FK_SOURCE_ID_BLEND_PROPS");
            });

            modelBuilder.Entity<AbcBlendSampleProps>(entity =>
            {
                entity.HasKey(e => new { e.BlendId, e.SampleName, e.PropId });

                entity.ToTable("ABC_BLEND_SAMPLE_PROPS");

                entity.Property(e => e.BlendId).HasColumnName("BLEND_ID");

                entity.Property(e => e.SampleName)
                    .HasColumnName("SAMPLE_NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PropId).HasColumnName("PROP_ID");

                entity.Property(e => e.AnzValue).HasColumnName("ANZ_VALUE");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Feedback).HasColumnName("FEEDBACK");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SetpointPred).HasColumnName("SETPOINT_PRED");

                entity.Property(e => e.UsedFlag)
                    .IsRequired()
                    .HasColumnName("USED_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.Value).HasColumnName("VALUE");

                entity.HasOne(d => d.Blend)
                    .WithMany(p => p.AbcBlendSampleProps)
                    .HasForeignKey(d => d.BlendId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BLEND_ID_SAMPLE_PROPS");

                entity.HasOne(d => d.Prop)
                    .WithMany(p => p.AbcBlendSampleProps)
                    .HasForeignKey(d => d.PropId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PROP_ID_SAMPLE_PROPS");
            });

            modelBuilder.Entity<AbcBlendSamples>(entity =>
            {
                entity.HasKey(e => new { e.BlendId, e.Name });

                entity.ToTable("ABC_BLEND_SAMPLES");

                entity.Property(e => e.BlendId).HasColumnName("BLEND_ID");

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.ProcessSampleFlag)
                    .HasColumnName("PROCESS_SAMPLE_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.StartDate)
                    .HasColumnName("START_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.StartVolume).HasColumnName("START_VOLUME");

                entity.Property(e => e.StopDate)
                    .HasColumnName("STOP_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.StopVolume).HasColumnName("STOP_VOLUME");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasColumnName("TYPE")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.HasOne(d => d.Blend)
                    .WithMany(p => p.AbcBlendSamples)
                    .HasForeignKey(d => d.BlendId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BLEND_ID_SAMPLES");
            });

            modelBuilder.Entity<AbcBlendSources>(entity =>
            {
                entity.HasKey(e => new { e.BlendId, e.MatId, e.TankId })
                    .HasName("PK_BLEND_SOURCE");

                entity.ToTable("ABC_BLEND_SOURCES");

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.BlendId).HasColumnName("BLEND_ID");

                entity.Property(e => e.MatId).HasColumnName("MAT_ID");

                entity.Property(e => e.TankId).HasColumnName("TANK_ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.InUseFlag)
                    .IsRequired()
                    .HasColumnName("IN_USE_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LineupId).HasColumnName("LINEUP_ID");

                entity.Property(e => e.MasterLineupId).HasColumnName("MASTER_LINEUP_ID");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.HasOne(d => d.Tank)
                    .WithMany(p => p.AbcBlendSources)
                    .HasForeignKey(d => d.TankId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TANK_ID_BLEND_SOURCE");

                entity.HasOne(d => d.AbcBlendComps)
                    .WithMany(p => p.AbcBlendSources)
                    .HasForeignKey(d => new { d.BlendId, d.MatId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BLEND_MAT_BLEND_SOURCE");
            });

            modelBuilder.Entity<AbcBlendStations>(entity =>
            {
                entity.HasKey(e => new { e.BlendId, e.StationId, e.MatId })
                    .HasName("PK_BLEND_STATIONS");

                entity.ToTable("ABC_BLEND_STATIONS");

                entity.Property(e => e.BlendId).HasColumnName("BLEND_ID");

                entity.Property(e => e.StationId).HasColumnName("STATION_ID");

                entity.Property(e => e.MatId).HasColumnName("MAT_ID");

                entity.Property(e => e.ActSetpoint).HasColumnName("ACT_SETPOINT");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.CurSetpoint).HasColumnName("CUR_SETPOINT");

                entity.Property(e => e.CurVol).HasColumnName("CUR_VOL");

                entity.Property(e => e.InUseFlag)
                    .IsRequired()
                    .HasColumnName("IN_USE_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.LastReadtime)
                    .HasColumnName("LAST_READTIME")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.MatFraction).HasColumnName("MAT_FRACTION");

                entity.Property(e => e.MaxFlow).HasColumnName("MAX_FLOW");

                entity.Property(e => e.MeasFlow).HasColumnName("MEAS_FLOW");

                entity.Property(e => e.MinFlow).HasColumnName("MIN_FLOW");

                entity.Property(e => e.PrevVol).HasColumnName("PREV_VOL");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.HasOne(d => d.Blend)
                    .WithMany(p => p.AbcBlendStations)
                    .HasForeignKey(d => d.BlendId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BLEND_ID_BLEND_STATIONS");

                entity.HasOne(d => d.Mat)
                    .WithMany(p => p.AbcBlendStations)
                    .HasForeignKey(d => d.MatId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MAT_ID_BLEND_STATIONS");

                entity.HasOne(d => d.Station)
                    .WithMany(p => p.AbcBlendStations)
                    .HasForeignKey(d => d.StationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_STATION_ID_BLEND_STATIONS");
            });

            modelBuilder.Entity<AbcBlendSwings>(entity =>
            {
                entity.HasKey(e => new { e.BlendId, e.FromTkId, e.ToTkId, e.DoneAt })
                    .HasName("PK_BLEND_SWINGS");

                entity.ToTable("ABC_BLEND_SWINGS");

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.BlendId).HasColumnName("BLEND_ID");

                entity.Property(e => e.FromTkId).HasColumnName("FROM_TK_ID");

                entity.Property(e => e.ToTkId).HasColumnName("TO_TK_ID");

                entity.Property(e => e.DoneAt)
                    .HasColumnName("DONE_AT")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.AutoSwingFlag)
                    .IsRequired()
                    .HasColumnName("AUTO_SWING_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.CriteriaId).HasColumnName("CRITERIA_ID");

                entity.Property(e => e.CriteriaNumLmt).HasColumnName("CRITERIA_NUM_LMT");

                entity.Property(e => e.CriteriaTimLmt)
                    .HasColumnName("CRITERIA_TIM_LMT")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SwingState)
                    .HasColumnName("SWING_STATE")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.SwingType)
                    .HasColumnName("SWING_TYPE")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.HasOne(d => d.Blend)
                    .WithMany(p => p.AbcBlendSwings)
                    .HasForeignKey(d => d.BlendId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BLENDID_BLNDSWINGS");

                entity.HasOne(d => d.FromTk)
                    .WithMany(p => p.AbcBlendSwingsFromTk)
                    .HasForeignKey(d => d.FromTkId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FRTKID_BLNDSWINGS");

                entity.HasOne(d => d.SwingStateNavigation)
                    .WithMany(p => p.AbcBlendSwings)
                    .HasForeignKey(d => d.SwingState)
                    .HasConstraintName("FK_SWING_STATE_BLEND_SWINGS");

                entity.HasOne(d => d.ToTk)
                    .WithMany(p => p.AbcBlendSwingsToTk)
                    .HasForeignKey(d => d.ToTkId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TOTKID_BLNDSWINGS");
            });

            modelBuilder.Entity<AbcBlenderComps>(entity =>
            {
                entity.HasKey(e => new { e.BlenderId, e.MatId })
                    .HasName("PK_BLENDER_COMPS");

                entity.ToTable("ABC_BLENDER_COMPS");

                entity.Property(e => e.BlenderId).HasColumnName("BLENDER_ID");

                entity.Property(e => e.MatId).HasColumnName("MAT_ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.DefaultTkId).HasColumnName("DEFAULT_TK_ID");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LineupFeedbackTid).HasColumnName("LINEUP_FEEDBACK_TID");

                entity.Property(e => e.LineupPreselTid).HasColumnName("LINEUP_PRESEL_TID");

                entity.Property(e => e.LineupSelTid).HasColumnName("LINEUP_SEL_TID");

                entity.Property(e => e.RecipeMeasTid).HasColumnName("RECIPE_MEAS_TID");

                entity.Property(e => e.RecipeSpTid).HasColumnName("RECIPE_SP_TID");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SelectCompTid).HasColumnName("SELECT_COMP_TID");

                entity.Property(e => e.SerialNo).HasColumnName("SERIAL_NO");

                entity.Property(e => e.SwingOccurredTid).HasColumnName("SWING_OCCURRED_TID");

                entity.Property(e => e.SwingTid).HasColumnName("SWING_TID");

                entity.Property(e => e.TotCompVolTid).HasColumnName("TOT_COMP_VOL_TID");

                entity.Property(e => e.TotflowControlTid).HasColumnName("TOTFLOW_CONTROL_TID");

                entity.Property(e => e.WildFlagTid).HasColumnName("WILD_FLAG_TID");

                entity.HasOne(d => d.Blender)
                    .WithMany(p => p.AbcBlenderComps)
                    .HasForeignKey(d => d.BlenderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BLENDER_ID_BLENDER_COMPS");

                entity.HasOne(d => d.DefaultTk)
                    .WithMany(p => p.AbcBlenderComps)
                    .HasForeignKey(d => d.DefaultTkId)
                    .HasConstraintName("FK_DEFAULT_TK_BLENDER_COMPS");

                entity.HasOne(d => d.LineupFeedbackT)
                    .WithMany(p => p.AbcBlenderCompsLineupFeedbackT)
                    .HasForeignKey(d => d.LineupFeedbackTid)
                    .HasConstraintName("FK_LINEUP_FB_BLENDER_COMPS");

                entity.HasOne(d => d.LineupPreselT)
                    .WithMany(p => p.AbcBlenderCompsLineupPreselT)
                    .HasForeignKey(d => d.LineupPreselTid)
                    .HasConstraintName("FK_LINEUP_PRESEL_BLENDER_COMPS");

                entity.HasOne(d => d.LineupSelT)
                    .WithMany(p => p.AbcBlenderCompsLineupSelT)
                    .HasForeignKey(d => d.LineupSelTid)
                    .HasConstraintName("FK_LINEUP_SEL_BLENDER_COMPS");

                entity.HasOne(d => d.Mat)
                    .WithMany(p => p.AbcBlenderComps)
                    .HasForeignKey(d => d.MatId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MAT_ID_BLENDER_COMPS");

                entity.HasOne(d => d.RecipeMeasT)
                    .WithMany(p => p.AbcBlenderCompsRecipeMeasT)
                    .HasForeignKey(d => d.RecipeMeasTid)
                    .HasConstraintName("FK_RECIPE_MEAS_BLENDER_COMPS");

                entity.HasOne(d => d.RecipeSpT)
                    .WithMany(p => p.AbcBlenderCompsRecipeSpT)
                    .HasForeignKey(d => d.RecipeSpTid)
                    .HasConstraintName("FK_RECIPE_SP_BLENDER_COMPS");

                entity.HasOne(d => d.SelectCompT)
                    .WithMany(p => p.AbcBlenderCompsSelectCompT)
                    .HasForeignKey(d => d.SelectCompTid)
                    .HasConstraintName("FK_SELECT_COMP_BLENDER_COMPS");

                entity.HasOne(d => d.SwingOccurredT)
                    .WithMany(p => p.AbcBlenderCompsSwingOccurredT)
                    .HasForeignKey(d => d.SwingOccurredTid)
                    .HasConstraintName("FK_SWING_OCRD_TID_BLDR_COMPS");

                entity.HasOne(d => d.SwingT)
                    .WithMany(p => p.AbcBlenderCompsSwingT)
                    .HasForeignKey(d => d.SwingTid)
                    .HasConstraintName("FK_SWING_TID_BLENDER_COMPS");

                entity.HasOne(d => d.TotCompVolT)
                    .WithMany(p => p.AbcBlenderCompsTotCompVolT)
                    .HasForeignKey(d => d.TotCompVolTid)
                    .HasConstraintName("FK_TOT_COMP_VOL_BLENDER_COMPS");

                entity.HasOne(d => d.WildFlagT)
                    .WithMany(p => p.AbcBlenderCompsWildFlagT)
                    .HasForeignKey(d => d.WildFlagTid)
                    .HasConstraintName("FK_WILD_FLAG_TID_BLENDER_COMPS");
            });

            modelBuilder.Entity<AbcBlenderDest>(entity =>
            {
                entity.HasKey(e => new { e.BlenderId, e.TankId })
                    .HasName("PK_BLENDER_DEST");

                entity.ToTable("ABC_BLENDER_DEST");

                entity.Property(e => e.BlenderId).HasColumnName("BLENDER_ID");

                entity.Property(e => e.TankId).HasColumnName("TANK_ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.DefaultLineupId).HasColumnName("DEFAULT_LINEUP_ID");

                entity.Property(e => e.DestSelectNameTid).HasColumnName("DEST_SELECT_NAME_TID");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.PreselectionTid).HasColumnName("PRESELECTION_TID");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SelectionFbTid).HasColumnName("SELECTION_FB_TID");

                entity.Property(e => e.SelectionTid).HasColumnName("SELECTION_TID");

                entity.HasOne(d => d.Blender)
                    .WithMany(p => p.AbcBlenderDest)
                    .HasForeignKey(d => d.BlenderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BLENDER_ID_BLENDER_DEST");

                entity.HasOne(d => d.DefaultLineup)
                    .WithMany(p => p.AbcBlenderDest)
                    .HasForeignKey(d => d.DefaultLineupId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DEF_LINEUP_BLENDER_DEST");

                entity.HasOne(d => d.DestSelectNameT)
                    .WithMany(p => p.AbcBlenderDestDestSelectNameT)
                    .HasForeignKey(d => d.DestSelectNameTid)
                    .HasConstraintName("FK_DEST_SEL_TID_BLENDER_DEST");

                entity.HasOne(d => d.PreselectionT)
                    .WithMany(p => p.AbcBlenderDestPreselectionT)
                    .HasForeignKey(d => d.PreselectionTid)
                    .HasConstraintName("FK_PRESELECTION_BLENDER_DEST");

                entity.HasOne(d => d.SelectionFbT)
                    .WithMany(p => p.AbcBlenderDestSelectionFbT)
                    .HasForeignKey(d => d.SelectionFbTid)
                    .HasConstraintName("FK_SELECTIONFB_BLENDER_DEST");

                entity.HasOne(d => d.SelectionT)
                    .WithMany(p => p.AbcBlenderDestSelectionT)
                    .HasForeignKey(d => d.SelectionTid)
                    .HasConstraintName("FK_SELECTION_BLENDER_DEST");

                entity.HasOne(d => d.Tank)
                    .WithMany(p => p.AbcBlenderDest)
                    .HasForeignKey(d => d.TankId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TANK_ID_BLENDER_DEST");
            });

            modelBuilder.Entity<AbcBlenderSources>(entity =>
            {
                entity.HasKey(e => new { e.BlenderId, e.TankId })
                    .HasName("PK_BLENDER_SOURCES");

                entity.ToTable("ABC_BLENDER_SOURCES");

                entity.Property(e => e.BlenderId).HasColumnName("BLENDER_ID");

                entity.Property(e => e.TankId).HasColumnName("TANK_ID");

                entity.Property(e => e.AltStorageMaxFlow).HasColumnName("ALT_STORAGE_MAX_FLOW");

                entity.Property(e => e.AltStorageMinFlow).HasColumnName("ALT_STORAGE_MIN_FLOW");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.DefaultLineupId).HasColumnName("DEFAULT_LINEUP_ID");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.PreselectionTid).HasColumnName("PRESELECTION_TID");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SelectionFbTid).HasColumnName("SELECTION_FB_TID");

                entity.Property(e => e.SelectionTid).HasColumnName("SELECTION_TID");

                entity.Property(e => e.StorageFlowTid).HasColumnName("STORAGE_FLOW_TID");

                entity.Property(e => e.UseStorageControl)
                    .HasColumnName("USE_STORAGE_CONTROL")
                    .HasMaxLength(3)
                    .IsUnicode(false);

                entity.HasOne(d => d.Blender)
                    .WithMany(p => p.AbcBlenderSources)
                    .HasForeignKey(d => d.BlenderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BLENDER_ID_BLENDER_SOURCES");

                entity.HasOne(d => d.DefaultLineup)
                    .WithMany(p => p.AbcBlenderSources)
                    .HasForeignKey(d => d.DefaultLineupId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DEF_LINEUP_BLENDER_SOURCES");

                entity.HasOne(d => d.PreselectionT)
                    .WithMany(p => p.AbcBlenderSourcesPreselectionT)
                    .HasForeignKey(d => d.PreselectionTid)
                    .HasConstraintName("FK_PRESEL_TID_BLENDER_SRCS");

                entity.HasOne(d => d.SelectionFbT)
                    .WithMany(p => p.AbcBlenderSourcesSelectionFbT)
                    .HasForeignKey(d => d.SelectionFbTid)
                    .HasConstraintName("FK_SELECTIONFB_BLENDER_SOURCES");

                entity.HasOne(d => d.SelectionT)
                    .WithMany(p => p.AbcBlenderSourcesSelectionT)
                    .HasForeignKey(d => d.SelectionTid)
                    .HasConstraintName("FK_SELECTION_BLENDER_SOURCES");

                entity.HasOne(d => d.StorageFlowT)
                    .WithMany(p => p.AbcBlenderSourcesStorageFlowT)
                    .HasForeignKey(d => d.StorageFlowTid)
                    .HasConstraintName("FK_STRG_FLOW_TID_BDR_SOURCES");

                entity.HasOne(d => d.Tank)
                    .WithMany(p => p.AbcBlenderSources)
                    .HasForeignKey(d => d.TankId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TANK_ID_BLENDER_SOURCES");
            });

            modelBuilder.Entity<AbcBlenders>(entity =>
            {
                entity.ToTable("ABC_BLENDERS");

                entity.HasIndex(e => e.CompositeTankId)
                    .HasName("UQ_COMP_TANK_BLENDERS")
                    .IsUnique();

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_BLENDERS")
                    .IsUnique();

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.HasIndex(e => e.SpotTankId)
                    .HasName("UQ_SPOT_TANK_BLENDERS")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.AnzrStartDelay).HasColumnName("ANZR_START_DELAY");

                entity.Property(e => e.BlendDescTid).HasColumnName("BLEND_DESC_TID");

                entity.Property(e => e.BlendIdTid).HasColumnName("BLEND_ID_TID");

                entity.Property(e => e.CalcpropFlag)
                    .HasColumnName("CALCPROP_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.Property(e => e.CommErrFlag)
                    .IsRequired()
                    .HasColumnName("COMM_ERR_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.CompositeTankId).HasColumnName("COMPOSITE_TANK_ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.DcsBlnameFbTid).HasColumnName("DCS_BLNAME_FB_TID");

                entity.Property(e => e.DebugDepth).HasColumnName("DEBUG_DEPTH");

                entity.Property(e => e.DebugFlag)
                    .IsRequired()
                    .HasColumnName("DEBUG_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.DebugLevel).HasColumnName("DEBUG_LEVEL");

                entity.Property(e => e.DeltaRecipe)
                    .HasColumnName("DELTA_RECIPE")
                    .HasDefaultValueSql("((5))");

                entity.Property(e => e.DownloadOkTid).HasColumnName("DOWNLOAD_OK_TID");

                entity.Property(e => e.DownloadType)
                    .HasColumnName("DOWNLOAD_TYPE")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.DownloadingTid).HasColumnName("DOWNLOADING_TID");

                entity.Property(e => e.Ectf)
                    .HasColumnName("ECTF")
                    .HasDefaultValueSql("((10))");

                entity.Property(e => e.EthanolFlag)
                    .IsRequired()
                    .HasColumnName("ETHANOL_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.FlowUomId).HasColumnName("FLOW_UOM_ID");

                entity.Property(e => e.GlPrdgrpId).HasColumnName("GL_PRDGRP_ID");

                entity.Property(e => e.GradeTid).HasColumnName("GRADE_TID");

                entity.Property(e => e.HdrVolume).HasColumnName("HDR_VOLUME");

                entity.Property(e => e.InSerFlag)
                    .IsRequired()
                    .HasColumnName("IN_SER_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LineupFeedbackTid).HasColumnName("LINEUP_FEEDBACK_TID");

                entity.Property(e => e.LineupPreselTid).HasColumnName("LINEUP_PRESEL_TID");

                entity.Property(e => e.LineupSelTid).HasColumnName("LINEUP_SEL_TID");

                entity.Property(e => e.LocalGlobalFlag)
                    .IsRequired()
                    .HasColumnName("LOCAL_GLOBAL_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('GLOBAL')");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.OfflineName)
                    .HasColumnName("OFFLINE_NAME")
                    .HasMaxLength(24)
                    .IsUnicode(false);

                entity.Property(e => e.OnSpecVol)
                    .HasColumnName("ON_SPEC_VOL")
                    .HasDefaultValueSql("((80))");

                entity.Property(e => e.OptimizeFlag)
                    .HasColumnName("OPTIMIZE_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.Property(e => e.PauseTid).HasColumnName("PAUSE_TID");

                entity.Property(e => e.PrdgrpId).HasColumnName("PRDGRP_ID");

                entity.Property(e => e.ProductTid).HasColumnName("PRODUCT_TID");

                entity.Property(e => e.ProgramError)
                    .HasColumnName("PROGRAM_ERROR")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PumpaSelTid).HasColumnName("PUMPA_SEL_TID");

                entity.Property(e => e.PumpbSelTid).HasColumnName("PUMPB_SEL_TID");

                entity.Property(e => e.PumpcSelTid).HasColumnName("PUMPC_SEL_TID");

                entity.Property(e => e.PumpdSelTid).HasColumnName("PUMPD_SEL_TID");

                entity.Property(e => e.RbcModeTid).HasColumnName("RBC_MODE_TID");

                entity.Property(e => e.RbcRateSpFbTid).HasColumnName("RBC_RATE_SP_FB_TID");

                entity.Property(e => e.RbcStateTid).HasColumnName("RBC_STATE_TID");

                entity.Property(e => e.RbcVolSpFbTid).HasColumnName("RBC_VOL_SP_FB_TID");

                entity.Property(e => e.RbcWdogTid).HasColumnName("RBC_WDOG_TID");

                entity.Property(e => e.RestartTid).HasColumnName("RESTART_TID");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.RundnFlag)
                    .IsRequired()
                    .HasColumnName("RUNDN_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.SpotTankId).HasColumnName("SPOT_TANK_ID");

                entity.Property(e => e.StarblendBiasType)
                    .HasColumnName("STARBLEND_BIAS_TYPE")
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.StartOkTid).HasColumnName("START_OK_TID");

                entity.Property(e => e.StartTid).HasColumnName("START_TID");

                entity.Property(e => e.StopOptVol).HasColumnName("STOP_OPT_VOL");

                entity.Property(e => e.StopTid).HasColumnName("STOP_TID");

                entity.Property(e => e.SwingExistTid).HasColumnName("SWING_EXIST_TID");

                entity.Property(e => e.SwingExitTid).HasColumnName("SWING_EXIT_TID");

                entity.Property(e => e.SwingOccuredTid).HasColumnName("SWING_OCCURED_TID");

                entity.Property(e => e.SwingOccurredTid).HasColumnName("SWING_OCCURRED_TID");

                entity.Property(e => e.SwingTid).HasColumnName("SWING_TID");

                entity.Property(e => e.SwingVolTid).HasColumnName("SWING_VOL_TID");

                entity.Property(e => e.TankFeedbackTid).HasColumnName("TANK_FEEDBACK_TID");

                entity.Property(e => e.TankPreselTid).HasColumnName("TANK_PRESEL_TID");

                entity.Property(e => e.TankSelTid).HasColumnName("TANK_SEL_TID");

                entity.Property(e => e.TargRateTid).HasColumnName("TARG_RATE_TID");

                entity.Property(e => e.TargVolTid).HasColumnName("TARG_VOL_TID");

                entity.Property(e => e.TotalFlowId).HasColumnName("TOTAL_FLOW_ID");

                entity.Property(e => e.TotalFlowTid).HasColumnName("TOTAL_FLOW_TID");

                entity.Property(e => e.TotalVolTid).HasColumnName("TOTAL_VOL_TID");

                entity.Property(e => e.VolFudgeFactor)
                    .HasColumnName("VOL_FUDGE_FACTOR")
                    .HasDefaultValueSql("((10))");

                entity.HasOne(d => d.BlendDescT)
                    .WithMany(p => p.AbcBlendersBlendDescT)
                    .HasForeignKey(d => d.BlendDescTid)
                    .HasConstraintName("FK_BLEND_DESC_TID_BLENDERS");

                entity.HasOne(d => d.BlendIdT)
                    .WithMany(p => p.AbcBlendersBlendIdT)
                    .HasForeignKey(d => d.BlendIdTid)
                    .HasConstraintName("FK_BLEND_ID_TID_BLENDERS");

                entity.HasOne(d => d.CompositeTank)
                    .WithOne(p => p.AbcBlendersCompositeTank)
                    .HasForeignKey<AbcBlenders>(d => d.CompositeTankId)
                    .HasConstraintName("FK_COMP_TANK_BLENDERS");

                entity.HasOne(d => d.DcsBlnameFbT)
                    .WithMany(p => p.AbcBlendersDcsBlnameFbT)
                    .HasForeignKey(d => d.DcsBlnameFbTid)
                    .HasConstraintName("FK_BLNAME_FB_BLENDERS");

                entity.HasOne(d => d.DownloadOkT)
                    .WithMany(p => p.AbcBlendersDownloadOkT)
                    .HasForeignKey(d => d.DownloadOkTid)
                    .HasConstraintName("FK_DOWNLOAD_OK_TID_BLENDERS");

                entity.HasOne(d => d.DownloadingT)
                    .WithMany(p => p.AbcBlendersDownloadingT)
                    .HasForeignKey(d => d.DownloadingTid)
                    .HasConstraintName("FK_DOWNLOADING_TID_BLENDERS");

                entity.HasOne(d => d.FlowUom)
                    .WithMany(p => p.AbcBlenders)
                    .HasForeignKey(d => d.FlowUomId)
                    .HasConstraintName("FK_UOM_ID_BLENDERS");

                entity.HasOne(d => d.GradeT)
                    .WithMany(p => p.AbcBlendersGradeT)
                    .HasForeignKey(d => d.GradeTid)
                    .HasConstraintName("FK_BLENDERS_GRADE_TAGS");

                entity.HasOne(d => d.LineupFeedbackT)
                    .WithMany(p => p.AbcBlendersLineupFeedbackT)
                    .HasForeignKey(d => d.LineupFeedbackTid)
                    .HasConstraintName("FK_LINEUP_FB_BLENDERS");

                entity.HasOne(d => d.LineupPreselT)
                    .WithMany(p => p.AbcBlendersLineupPreselT)
                    .HasForeignKey(d => d.LineupPreselTid)
                    .HasConstraintName("FK_LINEUP_PRESEL_BLENDERS");

                entity.HasOne(d => d.LineupSelT)
                    .WithMany(p => p.AbcBlendersLineupSelT)
                    .HasForeignKey(d => d.LineupSelTid)
                    .HasConstraintName("FK_LINEUP_SEL_BLENDERS");

                entity.HasOne(d => d.PauseT)
                    .WithMany(p => p.AbcBlendersPauseT)
                    .HasForeignKey(d => d.PauseTid)
                    .HasConstraintName("FK_PAUSE_TID_BLENDERS");

                entity.HasOne(d => d.ProductT)
                    .WithMany(p => p.AbcBlendersProductT)
                    .HasForeignKey(d => d.ProductTid)
                    .HasConstraintName("FK_PRODUCT_TID_BLENDERS");

                entity.HasOne(d => d.PumpaSelT)
                    .WithMany(p => p.AbcBlendersPumpaSelT)
                    .HasForeignKey(d => d.PumpaSelTid)
                    .HasConstraintName("FK_PUMPA_SEL_BLENDERS");

                entity.HasOne(d => d.PumpbSelT)
                    .WithMany(p => p.AbcBlendersPumpbSelT)
                    .HasForeignKey(d => d.PumpbSelTid)
                    .HasConstraintName("FK_PUMPB_SEL_BLENDERS");

                entity.HasOne(d => d.PumpcSelT)
                    .WithMany(p => p.AbcBlendersPumpcSelT)
                    .HasForeignKey(d => d.PumpcSelTid)
                    .HasConstraintName("FK_PUMPC_SEL_BLENDERS");

                entity.HasOne(d => d.PumpdSelT)
                    .WithMany(p => p.AbcBlendersPumpdSelT)
                    .HasForeignKey(d => d.PumpdSelTid)
                    .HasConstraintName("FK_PUMPD_SEL_BLENDERS");

                entity.HasOne(d => d.RbcModeT)
                    .WithMany(p => p.AbcBlendersRbcModeT)
                    .HasForeignKey(d => d.RbcModeTid)
                    .HasConstraintName("FK_RBC_MODE_TID_BLENDERS");

                entity.HasOne(d => d.RbcRateSpFbT)
                    .WithMany(p => p.AbcBlendersRbcRateSpFbT)
                    .HasForeignKey(d => d.RbcRateSpFbTid)
                    .HasConstraintName("FK_RBC_RATE_SP_FB_TID_BLENDERS");

                entity.HasOne(d => d.RbcStateT)
                    .WithMany(p => p.AbcBlendersRbcStateT)
                    .HasForeignKey(d => d.RbcStateTid)
                    .HasConstraintName("FK_RBC_STATE_TID_BLENDERS");

                entity.HasOne(d => d.RbcVolSpFbT)
                    .WithMany(p => p.AbcBlendersRbcVolSpFbT)
                    .HasForeignKey(d => d.RbcVolSpFbTid)
                    .HasConstraintName("FK_RBC_VOL_SP_FB_TID_BLENDERS");

                entity.HasOne(d => d.RbcWdogT)
                    .WithMany(p => p.AbcBlendersRbcWdogT)
                    .HasForeignKey(d => d.RbcWdogTid)
                    .HasConstraintName("FK_RBC_WDOG_TID_BLENDERS");

                entity.HasOne(d => d.RestartT)
                    .WithMany(p => p.AbcBlendersRestartT)
                    .HasForeignKey(d => d.RestartTid)
                    .HasConstraintName("FK_RESTART_TID_BLENDERS");

                entity.HasOne(d => d.SpotTank)
                    .WithOne(p => p.AbcBlendersSpotTank)
                    .HasForeignKey<AbcBlenders>(d => d.SpotTankId)
                    .HasConstraintName("FK_SPOT_TANK_BLENDERS");

                entity.HasOne(d => d.StartOkT)
                    .WithMany(p => p.AbcBlendersStartOkT)
                    .HasForeignKey(d => d.StartOkTid)
                    .HasConstraintName("FK_START_OK_TID_BLENDERS");

                entity.HasOne(d => d.StartT)
                    .WithMany(p => p.AbcBlendersStartT)
                    .HasForeignKey(d => d.StartTid)
                    .HasConstraintName("FK_START_TID_BLENDERS");

                entity.HasOne(d => d.StopT)
                    .WithMany(p => p.AbcBlendersStopT)
                    .HasForeignKey(d => d.StopTid)
                    .HasConstraintName("FK_STOP_TID_BLENDERS");

                entity.HasOne(d => d.SwingExistT)
                    .WithMany(p => p.AbcBlendersSwingExistT)
                    .HasForeignKey(d => d.SwingExistTid)
                    .HasConstraintName("FK_SWING_EXIST_BLENDERS");

                entity.HasOne(d => d.SwingExitT)
                    .WithMany(p => p.AbcBlendersSwingExitT)
                    .HasForeignKey(d => d.SwingExitTid)
                    .HasConstraintName("FK_SWING_EXIT_BLENDERS");

                entity.HasOne(d => d.SwingOccurredT)
                    .WithMany(p => p.AbcBlendersSwingOccurredT)
                    .HasForeignKey(d => d.SwingOccurredTid)
                    .HasConstraintName("FK_SWING_OCCURRED_TID_BLENDERS");

                entity.HasOne(d => d.SwingT)
                    .WithMany(p => p.AbcBlendersSwingT)
                    .HasForeignKey(d => d.SwingTid)
                    .HasConstraintName("FK_SWING_TID_BLENDERS");

                entity.HasOne(d => d.SwingVolT)
                    .WithMany(p => p.AbcBlendersSwingVolT)
                    .HasForeignKey(d => d.SwingVolTid)
                    .HasConstraintName("FK_SWING_VOL_BLENDERS");

                entity.HasOne(d => d.TankFeedbackT)
                    .WithMany(p => p.AbcBlendersTankFeedbackT)
                    .HasForeignKey(d => d.TankFeedbackTid)
                    .HasConstraintName("FK_TANK_FB_BLENDERS");

                entity.HasOne(d => d.TankPreselT)
                    .WithMany(p => p.AbcBlendersTankPreselT)
                    .HasForeignKey(d => d.TankPreselTid)
                    .HasConstraintName("FK_TANK_PRESEL_BLENDERS");

                entity.HasOne(d => d.TankSelT)
                    .WithMany(p => p.AbcBlendersTankSelT)
                    .HasForeignKey(d => d.TankSelTid)
                    .HasConstraintName("FK_TANK_SEL_BLENDERS");

                entity.HasOne(d => d.TargRateT)
                    .WithMany(p => p.AbcBlendersTargRateT)
                    .HasForeignKey(d => d.TargRateTid)
                    .HasConstraintName("FK_TARG_RATE_TID_BLENDERS");

                entity.HasOne(d => d.TargVolT)
                    .WithMany(p => p.AbcBlendersTargVolT)
                    .HasForeignKey(d => d.TargVolTid)
                    .HasConstraintName("FK_TARG_VOL_TID_BLENDERS");

                entity.HasOne(d => d.TotalFlowT)
                    .WithMany(p => p.AbcBlendersTotalFlowT)
                    .HasForeignKey(d => d.TotalFlowTid)
                    .HasConstraintName("FK_TOTAL_FLOW_TID_BLENDERS");

                entity.HasOne(d => d.TotalVolT)
                    .WithMany(p => p.AbcBlendersTotalVolT)
                    .HasForeignKey(d => d.TotalVolTid)
                    .HasConstraintName("FK_TOTAL_VOL_TID_BLENDERS");
            });

            modelBuilder.Entity<AbcBlends>(entity =>
            {
                entity.ToTable("ABC_BLENDS");

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_BLENDS")
                    .IsUnique();

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.ActualEnd)
                    .HasColumnName("ACTUAL_END")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.ActualStart)
                    .HasColumnName("ACTUAL_START")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Batch)
                    .HasColumnName("BATCH")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.BatchTargetVol).HasColumnName("BATCH_TARGET_VOL");

                entity.Property(e => e.BiasOverrideFlag)
                    .IsRequired()
                    .HasColumnName("BIAS_OVERRIDE_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.BlendState)
                    .HasColumnName("BLEND_STATE")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.BlendUsers).HasColumnName("BLEND_USERS");

                entity.Property(e => e.BlenderId).HasColumnName("BLENDER_ID");

                entity.Property(e => e.Comments)
                    .HasColumnName("COMMENTS")
                    .HasMaxLength(2000)
                    .IsUnicode(false);

                entity.Property(e => e.ControlMode)
                    .IsRequired()
                    .HasColumnName("CONTROL_MODE")
                    .HasMaxLength(12)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('AUTO')");

                entity.Property(e => e.CorrectionFactor).HasColumnName("CORRECTION_FACTOR");

                entity.Property(e => e.Cost).HasColumnName("COST");

                entity.Property(e => e.CostUomId).HasColumnName("COST_UOM_ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.CurrentVol).HasColumnName("CURRENT_VOL");

                entity.Property(e => e.DesOnspecVol).HasColumnName("DES_ONSPEC_VOL");

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(120)
                    .IsUnicode(false);

                entity.Property(e => e.EthanolBldgReqdFlag)
                    .HasColumnName("ETHANOL_BLDG_REQD_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false);

                entity.Property(e => e.ExpectedEnd)
                    .HasColumnName("EXPECTED_END")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.FlowUomId).HasColumnName("FLOW_UOM_ID");

                entity.Property(e => e.GradeId).HasColumnName("GRADE_ID");

                entity.Property(e => e.HdrPropConstraints)
                    .HasColumnName("HDR_PROP_CONSTRAINTS")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('PRODUCT LIMITS')");

                entity.Property(e => e.HeelUpdOccurredFlag)
                    .IsRequired()
                    .HasColumnName("HEEL_UPD_OCCURRED_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.IgnoreLineConstraints)
                    .IsRequired()
                    .HasColumnName("IGNORE_LINE_CONSTRAINTS")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.LastOptimizedTime)
                    .HasColumnName("LAST_OPTIMIZED_TIME")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LocalGlobalFlag)
                    .HasColumnName("LOCAL_GLOBAL_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false);

                entity.Property(e => e.MaxRate).HasColumnName("MAX_RATE");

                entity.Property(e => e.MaxVol).HasColumnName("MAX_VOL");

                entity.Property(e => e.MaximizeBlendRateFlag)
                    .IsRequired()
                    .HasColumnName("MAXIMIZE_BLEND_RATE_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.MinRate).HasColumnName("MIN_RATE");

                entity.Property(e => e.MinVol).HasColumnName("MIN_VOL");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Objective)
                    .HasColumnName("OBJECTIVE")
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.PacingActFlag)
                    .IsRequired()
                    .HasColumnName("PACING_ACT_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.PendingState)
                    .HasColumnName("PENDING_STATE")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.PlannedStart)
                    .HasColumnName("PLANNED_START")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.PoolingFlag)
                    .HasColumnName("POOLING_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.Property(e => e.PreviousBlendId).HasColumnName("PREVIOUS_BLEND_ID");

                entity.Property(e => e.ProductId).HasColumnName("PRODUCT_ID");

                entity.Property(e => e.RampingActFlag)
                    .IsRequired()
                    .HasColumnName("RAMPING_ACT_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.RateSpFb).HasColumnName("RATE_SP_FB");

                entity.Property(e => e.RateSpOp).HasColumnName("RATE_SP_OP");

                entity.Property(e => e.RcpDownloadTime)
                    .HasColumnName("RCP_DOWNLOAD_TIME")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SimulatedFlag)
                    .IsRequired()
                    .HasColumnName("SIMULATED_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.TargetRate).HasColumnName("TARGET_RATE");

                entity.Property(e => e.TargetVol).HasColumnName("TARGET_VOL");

                entity.Property(e => e.TqiNowFlag)
                    .HasColumnName("TQI_NOW_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.UpdateHeelFlag)
                    .IsRequired()
                    .HasColumnName("UPDATE_HEEL_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.Property(e => e.UseWildFlowFlag)
                    .IsRequired()
                    .HasColumnName("USE_WILD_FLOW_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.VolUomId).HasColumnName("VOL_UOM_ID");

                entity.Property(e => e.VolumeConstraints)
                    .IsRequired()
                    .HasColumnName("VOLUME_CONSTRAINTS")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.HasOne(d => d.Blender)
                    .WithMany(p => p.AbcBlends)
                    .HasForeignKey(d => d.BlenderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BLENDER_ID_BLENDS");

                entity.HasOne(d => d.CostUom)
                    .WithMany(p => p.AbcBlendsCostUom)
                    .HasForeignKey(d => d.CostUomId)
                    .HasConstraintName("FK_CUOM_ID_BLENDS");

                entity.HasOne(d => d.FlowUom)
                    .WithMany(p => p.AbcBlendsFlowUom)
                    .HasForeignKey(d => d.FlowUomId)
                    .HasConstraintName("FK_FUOM_ID_BLENDS");

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.AbcBlends)
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PRODUCT_ID_BLENDS");

                entity.HasOne(d => d.VolUom)
                    .WithMany(p => p.AbcBlendsVolUom)
                    .HasForeignKey(d => d.VolUomId)
                    .HasConstraintName("FK_VUOM_ID_BLENDS");
            });

            modelBuilder.Entity<AbcCalcCoefficients>(entity =>
            {
                entity.HasKey(e => new { e.CalcId, e.CoefOrder, e.PropId, e.PrdgrpId })
                    .HasName("PK_CALC_COEFFICIENTS");

                entity.ToTable("ABC_CALC_COEFFICIENTS");

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.CalcId).HasColumnName("CALC_ID");

                entity.Property(e => e.CoefOrder).HasColumnName("COEF_ORDER");

                entity.Property(e => e.PropId).HasColumnName("PROP_ID");

                entity.Property(e => e.PrdgrpId).HasColumnName("PRDGRP_ID");

                entity.Property(e => e.Coef).HasColumnName("COEF");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(72)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.HasOne(d => d.Calc)
                    .WithMany(p => p.AbcCalcCoefficients)
                    .HasForeignKey(d => d.CalcId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CALC_ID_CALC_COEFF");

                entity.HasOne(d => d.Prop)
                    .WithMany(p => p.AbcCalcCoefficients)
                    .HasForeignKey(d => d.PropId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PROP_ID_CALC_COEFF");
            });

            modelBuilder.Entity<AbcCalcRoutines>(entity =>
            {
                entity.ToTable("ABC_CALC_ROUTINES");

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_CALC_ROUTINES")
                    .IsUnique();

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.CoefType)
                    .HasColumnName("COEF_TYPE")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('PROPERTY ONLY')");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.OutputUomId).HasColumnName("OUTPUT_UOM_ID");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.HasOne(d => d.OutputUom)
                    .WithMany(p => p.AbcCalcRoutines)
                    .HasForeignKey(d => d.OutputUomId)
                    .HasConstraintName("FK_OUOM_ID_CALC");
            });

            modelBuilder.Entity<AbcCompLineupEqp>(entity =>
            {
                entity.HasKey(e => new { e.LineId, e.LineEqpOrder })
                    .HasName("PK_COMP_LINEUP_EQP");

                entity.ToTable("ABC_COMP_LINEUP_EQP");

                entity.Property(e => e.LineId).HasColumnName("LINE_ID");

                entity.Property(e => e.LineEqpOrder).HasColumnName("LINE_EQP_ORDER");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.PumpId).HasColumnName("PUMP_ID");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SelectionTid).HasColumnName("SELECTION_TID");

                entity.Property(e => e.StationId).HasColumnName("STATION_ID");

                entity.HasOne(d => d.Line)
                    .WithMany(p => p.AbcCompLineupEqp)
                    .HasForeignKey(d => d.LineId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LINE_ID_COMP_LINEUP_EQP");

                entity.HasOne(d => d.Pump)
                    .WithMany(p => p.AbcCompLineupEqp)
                    .HasForeignKey(d => d.PumpId)
                    .HasConstraintName("FK_PUMP_ID_COMP_LINEUP_EQP");

                entity.HasOne(d => d.SelectionT)
                    .WithMany(p => p.AbcCompLineupEqp)
                    .HasForeignKey(d => d.SelectionTid)
                    .HasConstraintName("FK_SELECTION_TID_COMP_LINE_EQP");

                entity.HasOne(d => d.Station)
                    .WithMany(p => p.AbcCompLineupEqp)
                    .HasForeignKey(d => d.StationId)
                    .HasConstraintName("FK_STATION_ID_COMP_LINEUP_EQP");
            });

            modelBuilder.Entity<AbcCompLineups>(entity =>
            {
                entity.ToTable("ABC_COMP_LINEUPS");

                entity.HasIndex(e => e.DcsLineupNum)
                    .HasName("SYS_C0013096")
                    .IsUnique();

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_COMP_LINEUPS")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.DcsLineupNum).HasColumnName("DCS_LINEUP_NUM");

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.DestinationId).HasColumnName("DESTINATION_ID");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LineGeoId).HasColumnName("LINE_GEO_ID");

                entity.Property(e => e.MaxFlow).HasColumnName("MAX_FLOW");

                entity.Property(e => e.MinFlow).HasColumnName("MIN_FLOW");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(25)
                    .IsUnicode(false);

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SelectionTid).HasColumnName("SELECTION_TID");

                entity.Property(e => e.SourceId).HasColumnName("SOURCE_ID");

                entity.Property(e => e.Volume).HasColumnName("VOLUME");

                entity.HasOne(d => d.Destination)
                    .WithMany(p => p.AbcCompLineups)
                    .HasForeignKey(d => d.DestinationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DESTINATION_ID_COMP_LINEUPS");

                entity.HasOne(d => d.LineGeo)
                    .WithMany(p => p.AbcCompLineups)
                    .HasForeignKey(d => d.LineGeoId)
                    .HasConstraintName("FK_LINE_GEO_ID_COMP_LINEUPS");

                entity.HasOne(d => d.SelectionT)
                    .WithMany(p => p.AbcCompLineups)
                    .HasForeignKey(d => d.SelectionTid)
                    .HasConstraintName("FK_SELECTION_TID_COMP_LINEUPS");

                entity.HasOne(d => d.Source)
                    .WithMany(p => p.AbcCompLineups)
                    .HasForeignKey(d => d.SourceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SOURCE_ID_COMP_LINEUPS");
            });

            modelBuilder.Entity<AbcGrades>(entity =>
            {
                entity.ToTable("ABC_GRADES");

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_GRADES")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");
            });

            modelBuilder.Entity<AbcIcons>(entity =>
            {
                entity.ToTable("ABC_ICONS");

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_ICONS")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.Icon)
                    .IsRequired()
                    .HasColumnName("ICON");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.VisibleFlag)
                    .IsRequired()
                    .HasColumnName("VISIBLE_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<AbcLabTankData>(entity =>
            {
                entity.HasKey(e => new { e.TankId, e.PropId })
                    .HasName("PK_LAB_TANK_DATA");

                entity.ToTable("ABC_LAB_TANK_DATA");

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.TankId).HasColumnName("TANK_ID");

                entity.Property(e => e.PropId).HasColumnName("PROP_ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.GoodFlag)
                    .HasColumnName("GOOD_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.LabValue).HasColumnName("LAB_VALUE");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SampleTime)
                    .HasColumnName("SAMPLE_TIME")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.UsageId).HasColumnName("USAGE_ID");

                entity.HasOne(d => d.Prop)
                    .WithMany(p => p.AbcLabTankData)
                    .HasForeignKey(d => d.PropId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PROP_ID_LAB_TANK_DATA");

                entity.HasOne(d => d.Tank)
                    .WithMany(p => p.AbcLabTankData)
                    .HasForeignKey(d => d.TankId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TANK_ID_LAB_TANK_DATA");

                entity.HasOne(d => d.Usage)
                    .WithMany(p => p.AbcLabTankData)
                    .HasForeignKey(d => d.UsageId)
                    .HasConstraintName("FK_USAGEID_LABTANKDATA");
            });

            modelBuilder.Entity<AbcLineupGeo>(entity =>
            {
                entity.ToTable("ABC_LINEUP_GEO");

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_LINEUP_GEO")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.IconId).HasColumnName("ICON_ID");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.NumOfPumps).HasColumnName("NUM_OF_PUMPS");

                entity.Property(e => e.NumOfStations).HasColumnName("NUM_OF_STATIONS");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.HasOne(d => d.Icon)
                    .WithMany(p => p.AbcLineupGeo)
                    .HasForeignKey(d => d.IconId)
                    .HasConstraintName("FK_ICON_ID_LINEUP_GEO");
            });

            modelBuilder.Entity<AbcMaterials>(entity =>
            {
                entity.ToTable("ABC_MATERIALS");

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_MATERIALS")
                    .IsUnique();

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.DcsMatNum).HasColumnName("DCS_MAT_NUM");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.PlanMatId)
                    .HasColumnName("PLAN_MAT_ID")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");
            });

            modelBuilder.Entity<AbcPrdAdditives>(entity =>
            {
                entity.HasKey(e => new { e.PrdgrpId, e.ProductId, e.AdditiveId })
                    .HasName("PK_PRD_ADDITIVES");

                entity.ToTable("ABC_PRD_ADDITIVES");

                entity.Property(e => e.PrdgrpId).HasColumnName("PRDGRP_ID");

                entity.Property(e => e.ProductId).HasColumnName("PRODUCT_ID");

                entity.Property(e => e.AdditiveId).HasColumnName("ADDITIVE_ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.DefaultSetpoint).HasColumnName("DEFAULT_SETPOINT");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.UnitFactor)
                    .HasColumnName("UNIT_FACTOR")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.UomId).HasColumnName("UOM_ID");

                entity.HasOne(d => d.Additive)
                    .WithMany(p => p.AbcPrdAdditivesAdditive)
                    .HasForeignKey(d => d.AdditiveId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ADID_PRD_ADDITIVES");

                entity.HasOne(d => d.Prdgrp)
                    .WithMany(p => p.AbcPrdAdditives)
                    .HasForeignKey(d => d.PrdgrpId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PRID_PRD_ADDITIVES");

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.AbcPrdAdditivesProduct)
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PGID_PRD_ADDITIVES");
            });

            modelBuilder.Entity<AbcPrdPropSpecs>(entity =>
            {
                entity.HasKey(e => new { e.PrdgrpId, e.MatId, e.PropId, e.GradeId })
                    .HasName("PK_PRD_PROP_SPECS");

                entity.ToTable("ABC_PRD_PROP_SPECS");

                entity.Property(e => e.PrdgrpId).HasColumnName("PRDGRP_ID");

                entity.Property(e => e.MatId).HasColumnName("MAT_ID");

                entity.Property(e => e.PropId).HasColumnName("PROP_ID");

                entity.Property(e => e.GradeId).HasColumnName("GRADE_ID");

                entity.Property(e => e.AnalysisMethod)
                    .HasColumnName("ANALYSIS_METHOD")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.BoDisplay)
                    .IsRequired()
                    .HasColumnName("BO_DISPLAY")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.Property(e => e.ControlMax).HasColumnName("CONTROL_MAX");

                entity.Property(e => e.ControlMin).HasColumnName("CONTROL_MIN");

                entity.Property(e => e.ControlSafetyMarginMax).HasColumnName("CONTROL_SAFETY_MARGIN_MAX");

                entity.Property(e => e.ControlSafetyMarginMin).HasColumnName("CONTROL_SAFETY_MARGIN_MIN");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Giveawaycost).HasColumnName("GIVEAWAYCOST");

                entity.Property(e => e.HlimMax).HasColumnName("HLIM_MAX");

                entity.Property(e => e.HlimMin).HasColumnName("HLIM_MIN");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SalesMax).HasColumnName("SALES_MAX");

                entity.Property(e => e.SalesMin).HasColumnName("SALES_MIN");

                entity.HasOne(d => d.Grade)
                    .WithMany(p => p.AbcPrdPropSpecs)
                    .HasForeignKey(d => d.GradeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_GRADE_ID_PRD_PROP_SPECS");

                entity.HasOne(d => d.Mat)
                    .WithMany(p => p.AbcPrdPropSpecs)
                    .HasForeignKey(d => d.MatId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MAT_ID_PRD_PROP_SPECS");

                entity.HasOne(d => d.Prdgrp)
                    .WithMany(p => p.AbcPrdPropSpecs)
                    .HasForeignKey(d => d.PrdgrpId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PRDGRP_ID_PRD_PROP_SPECS");

                entity.HasOne(d => d.Prop)
                    .WithMany(p => p.AbcPrdPropSpecs)
                    .HasForeignKey(d => d.PropId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PROP_ID_PRD_PROP_SPECS");
            });

            modelBuilder.Entity<AbcPrdgrpMatProps>(entity =>
            {
                entity.HasKey(e => new { e.PrdgrpId, e.MatId, e.UsageId, e.PropId })
                    .HasName("PK_PRDGRP_MAT_PROPS");

                entity.ToTable("ABC_PRDGRP_MAT_PROPS");

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.PrdgrpId).HasColumnName("PRDGRP_ID");

                entity.Property(e => e.MatId).HasColumnName("MAT_ID");

                entity.Property(e => e.UsageId).HasColumnName("USAGE_ID");

                entity.Property(e => e.PropId).HasColumnName("PROP_ID");

                entity.Property(e => e.CorrelationBias).HasColumnName("CORRELATION_BIAS");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.DefVal).HasColumnName("DEF_VAL");

                entity.Property(e => e.LabDisplay)
                    .IsRequired()
                    .HasColumnName("LAB_DISPLAY")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.PoolVal).HasColumnName("POOL_VAL");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.ValidMax).HasColumnName("VALID_MAX");

                entity.Property(e => e.ValidMin).HasColumnName("VALID_MIN");

                entity.HasOne(d => d.Mat)
                    .WithMany(p => p.AbcPrdgrpMatProps)
                    .HasForeignKey(d => d.MatId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MAT_ID_PRDGRP_MAT_PROPS");

                entity.HasOne(d => d.Prop)
                    .WithMany(p => p.AbcPrdgrpMatProps)
                    .HasForeignKey(d => d.PropId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PROP_ID_PRDGRP_MAT_PROPS");

                entity.HasOne(d => d.Usage)
                    .WithMany(p => p.AbcPrdgrpMatProps)
                    .HasForeignKey(d => d.UsageId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_USAGE_ID_PRDGRP_MAT_PROPS");

                entity.HasOne(d => d.Pr)
                    .WithMany(p => p.AbcPrdgrpMatProps)
                    .HasForeignKey(d => new { d.PrdgrpId, d.PropId })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PG_PID_PRDGRP_MAT_PROPS");
            });

            modelBuilder.Entity<AbcPrdgrpProps>(entity =>
            {
                entity.HasKey(e => new { e.PrdgrpId, e.PropId })
                    .HasName("PK_PRDGRP_PROPS");

                entity.ToTable("ABC_PRDGRP_PROPS");

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.PrdgrpId).HasColumnName("PRDGRP_ID");

                entity.Property(e => e.PropId).HasColumnName("PROP_ID");

                entity.Property(e => e.AltBiascalcDefault)
                    .IsRequired()
                    .HasColumnName("ALT_BIASCALC_DEFAULT")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('COMPOSITE')");

                entity.Property(e => e.BiascalcAnzFallback)
                    .IsRequired()
                    .HasColumnName("BIASCALC_ANZ_FALLBACK")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NONE')");

                entity.Property(e => e.BiascalcDefault)
                    .IsRequired()
                    .HasColumnName("BIASCALC_DEFAULT")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('ANALYZER')");

                entity.Property(e => e.CalcId).HasColumnName("CALC_ID");

                entity.Property(e => e.CompositeBiasClamp).HasColumnName("COMPOSITE_BIAS_CLAMP");

                entity.Property(e => e.CompositeFilter).HasColumnName("COMPOSITE_FILTER");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Giveawaycost).HasColumnName("GIVEAWAYCOST");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.MaxBias).HasColumnName("MAX_BIAS");

                entity.Property(e => e.MaxNegErr).HasColumnName("MAX_NEG_ERR");

                entity.Property(e => e.MaxPosErr).HasColumnName("MAX_POS_ERR");

                entity.Property(e => e.MinBias).HasColumnName("MIN_BIAS");

                entity.Property(e => e.PrefSpec)
                    .IsRequired()
                    .HasColumnName("PREF_SPEC")
                    .HasMaxLength(4)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NONE')");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SortOrder).HasColumnName("SORT_ORDER");

                entity.Property(e => e.SpotBiasClamp).HasColumnName("SPOT_BIAS_CLAMP");

                entity.Property(e => e.SpotFilter).HasColumnName("SPOT_FILTER");

                entity.Property(e => e.ValidCompMax).HasColumnName("VALID_COMP_MAX");

                entity.Property(e => e.ValidCompMin).HasColumnName("VALID_COMP_MIN");

                entity.Property(e => e.ValidPrdMax).HasColumnName("VALID_PRD_MAX");

                entity.Property(e => e.ValidPrdMin).HasColumnName("VALID_PRD_MIN");

                entity.HasOne(d => d.Calc)
                    .WithMany(p => p.AbcPrdgrpProps)
                    .HasForeignKey(d => d.CalcId)
                    .HasConstraintName("FK_CALC_ID_PRDGRP_PROPS");

                entity.HasOne(d => d.Prop)
                    .WithMany(p => p.AbcPrdgrpProps)
                    .HasForeignKey(d => d.PropId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PROP_ID_PRDGRP_PROPS");
            });

            modelBuilder.Entity<AbcPrdgrpUsages>(entity =>
            {
                entity.HasKey(e => new { e.PrdgrpId, e.MatId, e.UsageId })
                    .HasName("PK_PRDGRP_USAGES");

                entity.ToTable("ABC_PRDGRP_USAGES");

                entity.Property(e => e.PrdgrpId).HasColumnName("PRDGRP_ID");

                entity.Property(e => e.MatId).HasColumnName("MAT_ID");

                entity.Property(e => e.UsageId).HasColumnName("USAGE_ID");

                entity.Property(e => e.Cost).HasColumnName("COST");

                entity.Property(e => e.CostUomId).HasColumnName("COST_UOM_ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.NegDevCost).HasColumnName("NEG_DEV_COST");

                entity.Property(e => e.PosDevCost).HasColumnName("POS_DEV_COST");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.StarblendProductType)
                    .IsRequired()
                    .HasColumnName("STARBLEND_PRODUCT_TYPE")
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.HasOne(d => d.CostUom)
                    .WithMany(p => p.AbcPrdgrpUsages)
                    .HasForeignKey(d => d.CostUomId)
                    .HasConstraintName("FK_UOM_ID_PRDGRP_USAGES");

                entity.HasOne(d => d.Mat)
                    .WithMany(p => p.AbcPrdgrpUsages)
                    .HasForeignKey(d => d.MatId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MAT_ID_PRDGRP_USAGES");

                entity.HasOne(d => d.Prdgrp)
                    .WithMany(p => p.AbcPrdgrpUsages)
                    .HasForeignKey(d => d.PrdgrpId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PRDGRP_ID_PRDGRP_USAGES");

                entity.HasOne(d => d.Usage)
                    .WithMany(p => p.AbcPrdgrpUsages)
                    .HasForeignKey(d => d.UsageId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_USAGE_ID_PRDGRP_USAGES");
            });

            modelBuilder.Entity<AbcPrdgrps>(entity =>
            {
                entity.ToTable("ABC_PRDGRPS");

                entity.HasIndex(e => e.Alias)
                    .HasName("UQ_ALIAS_PRDGRPS")
                    .IsUnique();

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_PRDGRPS")
                    .IsUnique();

                entity.HasIndex(e => e.PlanPrdgrpId)
                    .HasName("UQ_PLAN_PRDGRP_TD_PRDGRPS")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Alias)
                    .IsRequired()
                    .HasColumnName("ALIAS")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.CycleTime).HasColumnName("CYCLE_TIME");

                entity.Property(e => e.FlowDenominator)
                    .IsRequired()
                    .HasColumnName("FLOW_DENOMINATOR")
                    .HasMaxLength(4)
                    .IsUnicode(false);

                entity.Property(e => e.IconId).HasColumnName("ICON_ID");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.PlanPrdgrpId)
                    .HasColumnName("PLAN_PRDGRP_ID")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.VolumeUomId).HasColumnName("VOLUME_UOM_ID");

                entity.HasOne(d => d.Icon)
                    .WithMany(p => p.AbcPrdgrps)
                    .HasForeignKey(d => d.IconId)
                    .HasConstraintName("FK_ICON_ID_PRDGRPS");

                entity.HasOne(d => d.VolumeUom)
                    .WithMany(p => p.AbcPrdgrps)
                    .HasForeignKey(d => d.VolumeUomId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_VOL_UOM_ID_PRDGRPS");
            });

            modelBuilder.Entity<AbcProdLineupEqp>(entity =>
            {
                entity.HasKey(e => new { e.LineId, e.LineEqpOrder })
                    .HasName("PK_PROD_LINEUP_EQP");

                entity.ToTable("ABC_PROD_LINEUP_EQP");

                entity.Property(e => e.LineId).HasColumnName("LINE_ID");

                entity.Property(e => e.LineEqpOrder).HasColumnName("LINE_EQP_ORDER");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.PumpId).HasColumnName("PUMP_ID");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SelectionTid).HasColumnName("SELECTION_TID");

                entity.Property(e => e.StationId).HasColumnName("STATION_ID");

                entity.Property(e => e.TransferLineId).HasColumnName("TRANSFER_LINE_ID");

                entity.HasOne(d => d.Line)
                    .WithMany(p => p.AbcProdLineupEqp)
                    .HasForeignKey(d => d.LineId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LINE_ID_PROD_LINEUP_EQP");

                entity.HasOne(d => d.Pump)
                    .WithMany(p => p.AbcProdLineupEqp)
                    .HasForeignKey(d => d.PumpId)
                    .HasConstraintName("FK_PUMP_ID_PROD_LINEUP_EQP");

                entity.HasOne(d => d.SelectionT)
                    .WithMany(p => p.AbcProdLineupEqp)
                    .HasForeignKey(d => d.SelectionTid)
                    .HasConstraintName("FK_SELECTION_TID_PROD_LINE_EQP");

                entity.HasOne(d => d.Station)
                    .WithMany(p => p.AbcProdLineupEqp)
                    .HasForeignKey(d => d.StationId)
                    .HasConstraintName("FK_STATION_ID_PROD_LINEUP_EQP");

                entity.HasOne(d => d.TransferLine)
                    .WithMany(p => p.AbcProdLineupEqp)
                    .HasForeignKey(d => d.TransferLineId)
                    .HasConstraintName("FK_XFR_LINE_PROD_LINEUP_EQP");
            });

            modelBuilder.Entity<AbcProdLineups>(entity =>
            {
                entity.ToTable("ABC_PROD_LINEUPS");

                entity.HasIndex(e => e.DcsLineupNum)
                    .HasName("SYS_C0013146")
                    .IsUnique();

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_PROD_LINEUPS")
                    .IsUnique();

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.DcsLineupNum).HasColumnName("DCS_LINEUP_NUM");

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.DestinationId).HasColumnName("DESTINATION_ID");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LineGeoId).HasColumnName("LINE_GEO_ID");

                entity.Property(e => e.MaxFlow).HasColumnName("MAX_FLOW");

                entity.Property(e => e.MinFlow).HasColumnName("MIN_FLOW");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PreselectionTid).HasColumnName("PRESELECTION_TID");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SelectionFbTid).HasColumnName("SELECTION_FB_TID");

                entity.Property(e => e.SelectionTid).HasColumnName("SELECTION_TID");

                entity.Property(e => e.SourceId).HasColumnName("SOURCE_ID");

                entity.Property(e => e.TransferLineId).HasColumnName("TRANSFER_LINE_ID");

                entity.Property(e => e.Volume).HasColumnName("VOLUME");

                entity.HasOne(d => d.Destination)
                    .WithMany(p => p.AbcProdLineupsDestination)
                    .HasForeignKey(d => d.DestinationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DESTINATION_ID_PROD_LINEUPS");

                entity.HasOne(d => d.PreselectionT)
                    .WithMany(p => p.AbcProdLineupsPreselectionT)
                    .HasForeignKey(d => d.PreselectionTid)
                    .HasConstraintName("FK_PRESEL_TID_PROD_LINEUPS");

                entity.HasOne(d => d.SelectionFbT)
                    .WithMany(p => p.AbcProdLineupsSelectionFbT)
                    .HasForeignKey(d => d.SelectionFbTid)
                    .HasConstraintName("FK_SELECTION_FB_TID_PROD_LUPS");

                entity.HasOne(d => d.SelectionT)
                    .WithMany(p => p.AbcProdLineupsSelectionT)
                    .HasForeignKey(d => d.SelectionTid)
                    .HasConstraintName("FK_SELECTION_TID_PROD_LINEUPS");

                entity.HasOne(d => d.Source)
                    .WithMany(p => p.AbcProdLineups)
                    .HasForeignKey(d => d.SourceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SOURCE_ID_PROD_LINEUPS");

                entity.HasOne(d => d.TransferLine)
                    .WithMany(p => p.AbcProdLineupsTransferLine)
                    .HasForeignKey(d => d.TransferLineId)
                    .HasConstraintName("FK_XFR_LINE_PROD_LINEUPS");
            });

            modelBuilder.Entity<AbcPrograms>(entity =>
            {
                entity.HasKey(e => e.Name)
                    .HasName("PK_PROGRAMS");

                entity.ToTable("ABC_PROGRAMS");

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Alias)
                    .IsRequired()
                    .HasColumnName("ALIAS")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.CommandLineArgs)
                    .HasColumnName("COMMAND_LINE_ARGS")
                    .HasMaxLength(132)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.CycleTime).HasColumnName("CYCLE_TIME");

                entity.Property(e => e.DebugFlag)
                    .IsRequired()
                    .HasColumnName("DEBUG_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.Property(e => e.DebugLevel).HasColumnName("DEBUG_LEVEL");

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(132)
                    .IsUnicode(false);

                entity.Property(e => e.EnabledFlag)
                    .IsRequired()
                    .HasColumnName("ENABLED_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.Property(e => e.LastRunTime)
                    .HasColumnName("LAST_RUN_TIME")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastStartTime)
                    .HasColumnName("LAST_START_TIME")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Path)
                    .HasColumnName("PATH")
                    .HasMaxLength(256)
                    .IsUnicode(false);

                entity.Property(e => e.RestartCounter).HasColumnName("RESTART_COUNTER");

                entity.Property(e => e.RestartLimit).HasColumnName("RESTART_LIMIT");

                entity.Property(e => e.RestartRequestFlag)
                    .IsRequired()
                    .HasColumnName("RESTART_REQUEST_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.StartupSequence)
                    .HasColumnName("STARTUP_SEQUENCE")
                    .HasDefaultValueSql("((10))");

                entity.Property(e => e.State)
                    .HasColumnName("STATE")
                    .HasMaxLength(20)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<AbcProjDefaults>(entity =>
            {
                entity.ToTable("ABC_PROJ_DEFAULTS");

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_PROJ_DEFAULTS")
                    .IsUnique();

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.AllowApDl)
                    .IsRequired()
                    .HasColumnName("ALLOW_AP_DL")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.AllowCompUpdates)
                    .IsRequired()
                    .HasColumnName("ALLOW_COMP_UPDATES")
                    .HasMaxLength(5)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.AllowRateAndVolUpdsFlag)
                    .IsRequired()
                    .HasColumnName("ALLOW_RATE_AND_VOL_UPDS_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.Property(e => e.AllowStartAndStopFlag)
                    .IsRequired()
                    .HasColumnName("ALLOW_START_AND_STOP_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.BatchBlendVolFlag)
                    .IsRequired()
                    .HasColumnName("BATCH_BLEND_VOL_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.BmonSleepTime)
                    .HasColumnName("BMON_SLEEP_TIME")
                    .HasDefaultValueSql("((15))");

                entity.Property(e => e.BoiActionAdd)
                    .HasColumnName("BOI_ACTION_ADD")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('ADD')");

                entity.Property(e => e.BoiActionDelete)
                    .HasColumnName("BOI_ACTION_DELETE")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('DELETE')");

                entity.Property(e => e.BoiActionReplace)
                    .HasColumnName("BOI_ACTION_REPLACE")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('REPLACE')");

                entity.Property(e => e.BoiBlend)
                    .HasColumnName("BOI_BLEND")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('BLEND')");

                entity.Property(e => e.BoiBlender)
                    .HasColumnName("BOI_BLENDER")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('BLENDER')");

                entity.Property(e => e.BoiComments)
                    .HasColumnName("BOI_COMMENTS")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('COMMENTS')");

                entity.Property(e => e.BoiComp)
                    .HasColumnName("BOI_COMP")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('COMPONENT')");

                entity.Property(e => e.BoiCompCost)
                    .HasColumnName("BOI_COMP_COST")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('COST')");

                entity.Property(e => e.BoiCompDevcost)
                    .HasColumnName("BOI_COMP_DEVCOST")
                    .HasMaxLength(24)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('DEVIATION COST')");

                entity.Property(e => e.BoiCompMax)
                    .HasColumnName("BOI_COMP_MAX")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('MAX')");

                entity.Property(e => e.BoiCompMin)
                    .HasColumnName("BOI_COMP_MIN")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('MIN')");

                entity.Property(e => e.BoiCompRcp)
                    .HasColumnName("BOI_COMP_RCP")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('RECIPE')");

                entity.Property(e => e.BoiDescription)
                    .HasColumnName("BOI_DESCRIPTION")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('DESCRIPTION')");

                entity.Property(e => e.BoiGrade)
                    .HasColumnName("BOI_GRADE")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('GRADE')");

                entity.Property(e => e.BoiProduct)
                    .HasColumnName("BOI_PRODUCT")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('PRODUCT')");

                entity.Property(e => e.BoiProp)
                    .HasColumnName("BOI_PROP")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('PROPERTY')");

                entity.Property(e => e.BoiPropGivecost)
                    .HasColumnName("BOI_PROP_GIVECOST")
                    .HasMaxLength(24)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('GIVEAWAY COST')");

                entity.Property(e => e.BoiPropHdrPred)
                    .HasColumnName("BOI_PROP_HDR_PRED")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('HDR PRED')");

                entity.Property(e => e.BoiPropMax)
                    .HasColumnName("BOI_PROP_MAX")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('MAX')");

                entity.Property(e => e.BoiPropMin)
                    .HasColumnName("BOI_PROP_MIN")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('MIN')");

                entity.Property(e => e.BoiPropTkFinalpred)
                    .HasColumnName("BOI_PROP_TK_FINALPRED")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('TK FINAL PRED')");

                entity.Property(e => e.BoiRate)
                    .HasColumnName("BOI_RATE")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('RATE')");

                entity.Property(e => e.BoiStart)
                    .HasColumnName("BOI_START")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('START')");

                entity.Property(e => e.BoiTank)
                    .HasColumnName("BOI_TANK")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('TANK')");

                entity.Property(e => e.BoiVolume)
                    .HasColumnName("BOI_VOLUME")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('VOLUME')");

                entity.Property(e => e.CopyInitrcpPrevFlag)
                    .IsRequired()
                    .HasColumnName("COPY_INITRCP_PREV_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.CorrectionFactor).HasColumnName("CORRECTION_FACTOR");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.CycleTime).HasColumnName("CYCLE_TIME");

                entity.Property(e => e.DcsCommFlag)
                    .IsRequired()
                    .HasColumnName("DCS_COMM_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.DownloadType)
                    .IsRequired()
                    .HasColumnName("DOWNLOAD_TYPE")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('STATION')");

                entity.Property(e => e.ErrTolerance)
                    .HasColumnName("ERR_TOLERANCE")
                    .HasDefaultValueSql("((0.01))");

                entity.Property(e => e.EtohPropsLabLimit)
                    .HasColumnName("ETOH_PROPS_LAB_LIMIT")
                    .HasDefaultValueSql("((3))");

                entity.Property(e => e.FgeEtoh)
                    .HasColumnName("FGE_ETOH")
                    .HasDefaultValueSql("((50))");

                entity.Property(e => e.FlowDenominator)
                    .IsRequired()
                    .HasColumnName("FLOW_DENOMINATOR")
                    .HasMaxLength(4)
                    .IsUnicode(false);

                entity.Property(e => e.FrozenOpLim).HasColumnName("FROZEN_OP_LIM");

                entity.Property(e => e.HdrPropConstraints)
                    .HasColumnName("HDR_PROP_CONSTRAINTS")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LimsAllowCompSampling)
                    .IsRequired()
                    .HasColumnName("LIMS_ALLOW_COMP_SAMPLING")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.LimsApprovedFlag)
                    .IsRequired()
                    .HasColumnName("LIMS_APPROVED_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.Property(e => e.LimsCompBlendIdField)
                    .HasColumnName("LIMS_COMP_BLEND_ID_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.LimsCompSampleIdField)
                    .HasColumnName("LIMS_COMP_SAMPLE_ID_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.LimsCompStartField)
                    .HasColumnName("LIMS_COMP_START_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.LimsCompStartStopType)
                    .IsRequired()
                    .HasColumnName("LIMS_COMP_START_STOP_TYPE")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('VOLUME')");

                entity.Property(e => e.LimsCompStopField)
                    .HasColumnName("LIMS_COMP_STOP_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.LimsDbName)
                    .IsRequired()
                    .HasColumnName("LIMS_DB_NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('LIMS')");

                entity.Property(e => e.LimsDbPath)
                    .IsRequired()
                    .HasColumnName("LIMS_DB_PATH")
                    .HasMaxLength(132)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('C:\\Program Files')");

                entity.Property(e => e.LimsDbType)
                    .IsRequired()
                    .HasColumnName("LIMS_DB_TYPE")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('ORACLE')");

                entity.Property(e => e.LimsId)
                    .IsRequired()
                    .HasColumnName("LIMS_ID")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('LIMS')");

                entity.Property(e => e.LimsLatestTimestamp)
                    .IsRequired()
                    .HasColumnName("LIMS_LATEST_TIMESTAMP")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.LimsMaxRecords)
                    .HasColumnName("LIMS_MAX_RECORDS")
                    .HasDefaultValueSql("((3000))");

                entity.Property(e => e.LimsPropField)
                    .IsRequired()
                    .HasColumnName("LIMS_PROP_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('PROP')");

                entity.Property(e => e.LimsPwd)
                    .IsRequired()
                    .HasColumnName("LIMS_PWD")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('LIMS')");

                entity.Property(e => e.LimsSampleField)
                    .IsRequired()
                    .HasColumnName("LIMS_SAMPLE_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('SAMPLE')");

                entity.Property(e => e.LimsSeparatePropsFlag)
                    .HasColumnName("LIMS_SEPARATE_PROPS_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.LimsServer)
                    .IsRequired()
                    .HasColumnName("LIMS_SERVER")
                    .HasMaxLength(32)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('LIMS')");

                entity.Property(e => e.LimsStatusField)
                    .HasColumnName("LIMS_STATUS_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('STATUS')");

                entity.Property(e => e.LimsTankField)
                    .IsRequired()
                    .HasColumnName("LIMS_TANK_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('TANK')");

                entity.Property(e => e.LimsTimestpField)
                    .IsRequired()
                    .HasColumnName("LIMS_TIMESTP_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('TIME_STAMP')");

                entity.Property(e => e.LimsValStrg)
                    .IsRequired()
                    .HasColumnName("LIMS_VAL_STRG")
                    .HasMaxLength(32)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('OK')");

                entity.Property(e => e.LimsViewName)
                    .IsRequired()
                    .HasColumnName("LIMS_VIEW_NAME")
                    .HasMaxLength(32)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('LIMS_ABC_VIEW')");

                entity.Property(e => e.LinePropertiesFlag)
                    .HasColumnName("LINE_PROPERTIES_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false);

                entity.Property(e => e.LogMsgKeepDays)
                    .HasColumnName("LOG_MSG_KEEP_DAYS")
                    .HasDefaultValueSql("((10))");

                entity.Property(e => e.MaxIntervalLen).HasColumnName("MAX_INTERVAL_LEN");

                entity.Property(e => e.MaxMatCost).HasColumnName("MAX_MAT_COST");

                entity.Property(e => e.MaxValveOpening)
                    .HasColumnName("MAX_VALVE_OPENING")
                    .HasDefaultValueSql("((95))");

                entity.Property(e => e.MessageType)
                    .HasColumnName("MESSAGE_TYPE")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.MinEtoh)
                    .HasColumnName("MIN_ETOH")
                    .HasDefaultValueSql("((0.5))");

                entity.Property(e => e.MinIntervalLen).HasColumnName("MIN_INTERVAL_LEN");

                entity.Property(e => e.MinMatCost).HasColumnName("MIN_MAT_COST");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.OmonSleepTime)
                    .HasColumnName("OMON_SLEEP_TIME")
                    .HasDefaultValueSql("((15))");

                entity.Property(e => e.OpnEngine)
                    .IsRequired()
                    .HasColumnName("OPN_ENGINE")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.OptValveConstraint)
                    .IsRequired()
                    .HasColumnName("OPT_VALVE_CONSTRAINT")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PlanBatchField)
                    .IsRequired()
                    .HasColumnName("PLAN_BATCH_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PlanDbName)
                    .IsRequired()
                    .HasColumnName("PLAN_DB_NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PlanDbPath)
                    .IsRequired()
                    .HasColumnName("PLAN_DB_PATH")
                    .HasMaxLength(132)
                    .IsUnicode(false);

                entity.Property(e => e.PlanDbType)
                    .IsRequired()
                    .HasColumnName("PLAN_DB_TYPE")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PlanDestField)
                    .IsRequired()
                    .HasColumnName("PLAN_DEST_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PlanId)
                    .IsRequired()
                    .HasColumnName("PLAN_ID")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PlanManagerField)
                    .IsRequired()
                    .HasColumnName("PLAN_MANAGER_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PlanMatField)
                    .IsRequired()
                    .HasColumnName("PLAN_MAT_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PlanMtypeField)
                    .IsRequired()
                    .HasColumnName("PLAN_MTYPE_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PlanPrdgrpField)
                    .IsRequired()
                    .HasColumnName("PLAN_PRDGRP_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PlanPwd)
                    .IsRequired()
                    .HasColumnName("PLAN_PWD")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PlanServer)
                    .IsRequired()
                    .HasColumnName("PLAN_SERVER")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.PlanSourceField)
                    .IsRequired()
                    .HasColumnName("PLAN_SOURCE_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PlanStartField)
                    .IsRequired()
                    .HasColumnName("PLAN_START_FIELD")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PlanViewName)
                    .IsRequired()
                    .HasColumnName("PLAN_VIEW_NAME")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.ProgCycleTime).HasColumnName("PROG_CYCLE_TIME");

                entity.Property(e => e.PropViolationTolerance)
                    .HasColumnName("PROP_VIOLATION_TOLERANCE")
                    .HasDefaultValueSql("((0.1))");

                entity.Property(e => e.RecipeTolerance)
                    .HasColumnName("RECIPE_TOLERANCE")
                    .HasDefaultValueSql("((0.01))");

                entity.Property(e => e.RecipeViolationTolerance).HasColumnName("RECIPE_VIOLATION_TOLERANCE");

                entity.Property(e => e.RefreshRate)
                    .HasColumnName("REFRESH_RATE")
                    .HasDefaultValueSql("((600))");

                entity.Property(e => e.ReportPaperType)
                    .HasColumnName("REPORT_PAPER_TYPE")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.Reserved01)
                    .HasColumnName("RESERVED01")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.Reserved02)
                    .HasColumnName("RESERVED02")
                    .HasMaxLength(3)
                    .IsUnicode(false);

                entity.Property(e => e.Reserved03)
                    .HasColumnName("RESERVED03")
                    .HasMaxLength(3)
                    .IsUnicode(false);

                entity.Property(e => e.Reserved04)
                    .HasColumnName("RESERVED04")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.ScaleOptVol).HasColumnName("SCALE_OPT_VOL");

                entity.Property(e => e.ScannerTagnameEscape)
                    .IsRequired()
                    .HasColumnName("SCANNER_TAGNAME_ESCAPE")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.StarblendInstPath)
                    .HasColumnName("STARBLEND_INST_PATH")
                    .HasMaxLength(132)
                    .IsUnicode(false);

                entity.Property(e => e.StartTimeout).HasColumnName("START_TIMEOUT");

                entity.Property(e => e.SwingTimeOut).HasColumnName("SWING_TIME_OUT");

                entity.Property(e => e.SystemId).HasColumnName("SYSTEM_ID");

                entity.Property(e => e.TankFractionTol)
                    .HasColumnName("TANK_FRACTION_TOL")
                    .HasDefaultValueSql("((0.1))");

                entity.Property(e => e.TmonSleepTime)
                    .HasColumnName("TMON_SLEEP_TIME")
                    .HasDefaultValueSql("((15))");

                entity.Property(e => e.TotalizerTimestampTolerance)
                    .HasColumnName("TOTALIZER_TIMESTAMP_TOLERANCE")
                    .HasDefaultValueSql("((30))");

                entity.Property(e => e.UserMonitorTid1).HasColumnName("USER_MONITOR_TID1");

                entity.Property(e => e.UserMonitorTid2).HasColumnName("USER_MONITOR_TID2");

                entity.Property(e => e.VarTargetRateFlag)
                    .IsRequired()
                    .HasColumnName("VAR_TARGET_RATE_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.Version)
                    .HasColumnName("VERSION")
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.VolTolerance).HasColumnName("VOL_TOLERANCE");

                entity.Property(e => e.VolumeUomId).HasColumnName("VOLUME_UOM_ID");

                entity.Property(e => e.WdogLimit).HasColumnName("WDOG_LIMIT");

                entity.Property(e => e.WdogTid).HasColumnName("WDOG_TID");

                entity.Property(e => e.ZeroRcpConstraintFlag)
                    .IsRequired()
                    .HasColumnName("ZERO_RCP_CONSTRAINT_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.HasOne(d => d.UserMonitorTid1Navigation)
                    .WithMany(p => p.AbcProjDefaultsUserMonitorTid1Navigation)
                    .HasForeignKey(d => d.UserMonitorTid1)
                    .HasConstraintName("FK_USER_MON_TID1_PROJ_DEFAULTS");

                entity.HasOne(d => d.UserMonitorTid2Navigation)
                    .WithMany(p => p.AbcProjDefaultsUserMonitorTid2Navigation)
                    .HasForeignKey(d => d.UserMonitorTid2)
                    .HasConstraintName("FK_USER_MON_TID2_PROJ_DEFAULTS");

                entity.HasOne(d => d.VolumeUom)
                    .WithMany(p => p.AbcProjDefaults)
                    .HasForeignKey(d => d.VolumeUomId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_VOL_UOM_ID_PROJ_DEF");

                entity.HasOne(d => d.WdogT)
                    .WithMany(p => p.AbcProjDefaultsWdogT)
                    .HasForeignKey(d => d.WdogTid)
                    .HasConstraintName("FK_WDOG_TID_PROJ_DEFAULTS");
            });

            modelBuilder.Entity<AbcPropSources>(entity =>
            {
                entity.ToTable("ABC_PROP_SOURCES");

                entity.HasIndex(e => e.Alias)
                    .HasName("UQ_ALIAS_PROP_SOURCES")
                    .IsUnique();

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_PROP_SOURCES")
                    .IsUnique();

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Alias)
                    .IsRequired()
                    .HasColumnName("ALIAS")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");
            });

            modelBuilder.Entity<AbcProperties>(entity =>
            {
                entity.ToTable("ABC_PROPERTIES");

                entity.HasIndex(e => e.Alias)
                    .HasName("UQ_ALIAS_PROPERTIES")
                    .IsUnique();

                entity.HasIndex(e => e.LimsPropName)
                    .HasName("SYS_C0013163")
                    .IsUnique();

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_PROPERTIES")
                    .IsUnique();

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.AbsMax).HasColumnName("ABS_MAX");

                entity.Property(e => e.AbsMin).HasColumnName("ABS_MIN");

                entity.Property(e => e.Alias)
                    .IsRequired()
                    .HasColumnName("ALIAS")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.DisplayDigits).HasColumnName("DISPLAY_DIGITS");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LimsPropName)
                    .HasColumnName("LIMS_PROP_NAME")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.OfflineName)
                    .HasColumnName("OFFLINE_NAME")
                    .HasMaxLength(24)
                    .IsUnicode(false);

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.StarblendSupportedFlag)
                    .HasColumnName("STARBLEND_SUPPORTED_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.UomId).HasColumnName("UOM_ID");

                entity.HasOne(d => d.Uom)
                    .WithMany(p => p.AbcProperties)
                    .HasForeignKey(d => d.UomId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UOM_ID_PROPERTIES");
            });

            modelBuilder.Entity<AbcPumps>(entity =>
            {
                entity.ToTable("ABC_PUMPS");

                entity.HasIndex(e => e.DcsPumpId)
                    .HasName("UQ_DCS_PUMP_ID_PUMPS")
                    .IsUnique();

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_PUMPS")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.DcsPumpId).HasColumnName("DCS_PUMP_ID");

                entity.Property(e => e.FlowUomId).HasColumnName("FLOW_UOM_ID");

                entity.Property(e => e.InSerFlag)
                    .IsRequired()
                    .HasColumnName("IN_SER_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.InuseTagId).HasColumnName("INUSE_TAG_ID");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Max).HasColumnName("MAX");

                entity.Property(e => e.Min).HasColumnName("MIN");

                entity.Property(e => e.ModeTid).HasColumnName("MODE_TID");

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.PrdgrpId).HasColumnName("PRDGRP_ID");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.StatusTagId).HasColumnName("STATUS_TAG_ID");

                entity.HasOne(d => d.FlowUom)
                    .WithMany(p => p.AbcPumps)
                    .HasForeignKey(d => d.FlowUomId)
                    .HasConstraintName("FK_FUOM_ID_PUMPS");

                entity.HasOne(d => d.InuseTag)
                    .WithMany(p => p.AbcPumpsInuseTag)
                    .HasForeignKey(d => d.InuseTagId)
                    .HasConstraintName("FK_INUSE_TAG_ID_PUMPS");

                entity.HasOne(d => d.ModeT)
                    .WithMany(p => p.AbcPumpsModeT)
                    .HasForeignKey(d => d.ModeTid)
                    .HasConstraintName("FK_MODE_TID_PUMPS");

                entity.HasOne(d => d.Prdgrp)
                    .WithMany(p => p.AbcPumps)
                    .HasForeignKey(d => d.PrdgrpId)
                    .HasConstraintName("FK_PRDGRP_ID_PUMPS");

                entity.HasOne(d => d.StatusTag)
                    .WithMany(p => p.AbcPumpsStatusTag)
                    .HasForeignKey(d => d.StatusTagId)
                    .HasConstraintName("FK_STATUS_TAG_ID_PUMPS");
            });

            modelBuilder.Entity<AbcRbcStates>(entity =>
            {
                entity.HasKey(e => e.Name)
                    .HasName("PK_RBC_STATES");

                entity.ToTable("ABC_RBC_STATES");

                entity.HasIndex(e => e.Alias)
                    .HasName("UQ_ALIAS_RBC_STATES")
                    .IsUnique();

                entity.HasIndex(e => e.Value)
                    .HasName("UQ_VALUE_RBC_STATES")
                    .IsUnique();

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Alias)
                    .IsRequired()
                    .HasColumnName("ALIAS")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(132)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.Value).HasColumnName("VALUE");
            });

            modelBuilder.Entity<AbcScanGroups>(entity =>
            {
                entity.ToTable("ABC_SCAN_GROUPS");

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.DebugFlag)
                    .IsRequired()
                    .HasColumnName("DEBUG_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.LastRefreshTime)
                    .HasColumnName("LAST_REFRESH_TIME")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.ReadOrWriteFlag)
                    .IsRequired()
                    .HasColumnName("READ_OR_WRITE_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('READ')");

                entity.Property(e => e.RefreshNowFlag)
                    .IsRequired()
                    .HasColumnName("REFRESH_NOW_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.ScanEnabledFlag)
                    .IsRequired()
                    .HasColumnName("SCAN_ENABLED_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.Property(e => e.Scanrate).HasColumnName("SCANRATE");

                entity.Property(e => e.SkipScans).HasColumnName("SKIP_SCANS");
            });

            modelBuilder.Entity<AbcStations>(entity =>
            {
                entity.ToTable("ABC_STATIONS");

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_STATIONS")
                    .IsUnique();

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.BlenderId).HasColumnName("BLENDER_ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.DcsStationNum).HasColumnName("DCS_STATION_NUM");

                entity.Property(e => e.FlowMeasTagId).HasColumnName("FLOW_MEAS_TAG_ID");

                entity.Property(e => e.FlowOpTagId).HasColumnName("FLOW_OP_TAG_ID");

                entity.Property(e => e.FlowSpTagId).HasColumnName("FLOW_SP_TAG_ID");

                entity.Property(e => e.FlowUomId).HasColumnName("FLOW_UOM_ID");

                entity.Property(e => e.InUseFlag)
                    .IsRequired()
                    .HasColumnName("IN_USE_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LineupFeedbackTid).HasColumnName("LINEUP_FEEDBACK_TID");

                entity.Property(e => e.LineupPreselTid).HasColumnName("LINEUP_PRESEL_TID");

                entity.Property(e => e.LineupSelTid).HasColumnName("LINEUP_SEL_TID");

                entity.Property(e => e.MatNumTid).HasColumnName("MAT_NUM_TID");

                entity.Property(e => e.Max).HasColumnName("MAX");

                entity.Property(e => e.Min).HasColumnName("MIN");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.PaceMeFlagTid).HasColumnName("PACE_ME_FLAG_TID");

                entity.Property(e => e.PumpaSelTid).HasColumnName("PUMPA_SEL_TID");

                entity.Property(e => e.PumpbSelTid).HasColumnName("PUMPB_SEL_TID");

                entity.Property(e => e.PumpcSelTid).HasColumnName("PUMPC_SEL_TID");

                entity.Property(e => e.PumpdSelTid).HasColumnName("PUMPD_SEL_TID");

                entity.Property(e => e.RcpMeasTagId).HasColumnName("RCP_MEAS_TAG_ID");

                entity.Property(e => e.RcpSp).HasColumnName("RCP_SP");

                entity.Property(e => e.RcpSpTagId).HasColumnName("RCP_SP_TAG_ID");

                entity.Property(e => e.RecipeUomId).HasColumnName("RECIPE_UOM_ID");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SelectStationTid).HasColumnName("SELECT_STATION_TID");

                entity.Property(e => e.TankFeedbackTid).HasColumnName("TANK_FEEDBACK_TID");

                entity.Property(e => e.TankPreselectNumTid).HasColumnName("TANK_PRESELECT_NUM_TID");

                entity.Property(e => e.TankSelectNumTid).HasColumnName("TANK_SELECT_NUM_TID");

                entity.Property(e => e.TotalFlowControlTid).HasColumnName("TOTAL_FLOW_CONTROL_TID");

                entity.Property(e => e.TotalStationVolTid).HasColumnName("TOTAL_STATION_VOL_TID");

                entity.Property(e => e.VolUomId).HasColumnName("VOL_UOM_ID");

                entity.Property(e => e.WildFlagTid).HasColumnName("WILD_FLAG_TID");

                entity.HasOne(d => d.Blender)
                    .WithMany(p => p.AbcStations)
                    .HasForeignKey(d => d.BlenderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BLENDER_ID_STATIONS");

                entity.HasOne(d => d.FlowMeasTag)
                    .WithMany(p => p.AbcStationsFlowMeasTag)
                    .HasForeignKey(d => d.FlowMeasTagId)
                    .HasConstraintName("FK_FLOW_METAGID_STONS");

                entity.HasOne(d => d.FlowOpTag)
                    .WithMany(p => p.AbcStationsFlowOpTag)
                    .HasForeignKey(d => d.FlowOpTagId)
                    .HasConstraintName("FK_FLOW_OPTAGID_STONS");

                entity.HasOne(d => d.FlowSpTag)
                    .WithMany(p => p.AbcStationsFlowSpTag)
                    .HasForeignKey(d => d.FlowSpTagId)
                    .HasConstraintName("FK_FLOW_SPTAGID_STONS");

                entity.HasOne(d => d.FlowUom)
                    .WithMany(p => p.AbcStationsFlowUom)
                    .HasForeignKey(d => d.FlowUomId)
                    .HasConstraintName("FK_FUOM_ID_STONS");

                entity.HasOne(d => d.LineupFeedbackT)
                    .WithMany(p => p.AbcStationsLineupFeedbackT)
                    .HasForeignKey(d => d.LineupFeedbackTid)
                    .HasConstraintName("FK_LINEUP_FB_STATIONS");

                entity.HasOne(d => d.LineupPreselT)
                    .WithMany(p => p.AbcStationsLineupPreselT)
                    .HasForeignKey(d => d.LineupPreselTid)
                    .HasConstraintName("FK_LINEUP_PRESEL_STATIONS");

                entity.HasOne(d => d.LineupSelT)
                    .WithMany(p => p.AbcStationsLineupSelT)
                    .HasForeignKey(d => d.LineupSelTid)
                    .HasConstraintName("FK_LINEUP_SEL_STATIONS");

                entity.HasOne(d => d.MatNumT)
                    .WithMany(p => p.AbcStationsMatNumT)
                    .HasForeignKey(d => d.MatNumTid)
                    .HasConstraintName("FK_MAT_NUM_TID_STATIONS");

                entity.HasOne(d => d.PaceMeFlagT)
                    .WithMany(p => p.AbcStationsPaceMeFlagT)
                    .HasForeignKey(d => d.PaceMeFlagTid)
                    .HasConstraintName("FK_PACE_ME_FLAG_TID_STATIONS");

                entity.HasOne(d => d.PumpaSelT)
                    .WithMany(p => p.AbcStationsPumpaSelT)
                    .HasForeignKey(d => d.PumpaSelTid)
                    .HasConstraintName("FK_PUMPA_SEL_STATIONS");

                entity.HasOne(d => d.PumpbSelT)
                    .WithMany(p => p.AbcStationsPumpbSelT)
                    .HasForeignKey(d => d.PumpbSelTid)
                    .HasConstraintName("FK_PUMPB_SEL_STATIONS");

                entity.HasOne(d => d.PumpcSelT)
                    .WithMany(p => p.AbcStationsPumpcSelT)
                    .HasForeignKey(d => d.PumpcSelTid)
                    .HasConstraintName("FK_PUMPC_SEL_STATIONS");

                entity.HasOne(d => d.PumpdSelT)
                    .WithMany(p => p.AbcStationsPumpdSelT)
                    .HasForeignKey(d => d.PumpdSelTid)
                    .HasConstraintName("FK_PUMPD_SEL_STATIONS");

                entity.HasOne(d => d.RcpMeasTag)
                    .WithMany(p => p.AbcStationsRcpMeasTag)
                    .HasForeignKey(d => d.RcpMeasTagId)
                    .HasConstraintName("FK_FLOW_RCPMSTAGID_STONS");

                entity.HasOne(d => d.RcpSpTag)
                    .WithMany(p => p.AbcStationsRcpSpTag)
                    .HasForeignKey(d => d.RcpSpTagId)
                    .HasConstraintName("FK_FLOW_RCPSPTAGID_STONS");

                entity.HasOne(d => d.RecipeUom)
                    .WithMany(p => p.AbcStationsRecipeUom)
                    .HasForeignKey(d => d.RecipeUomId)
                    .HasConstraintName("FK_RUOM_ID_STONS");

                entity.HasOne(d => d.SelectStationT)
                    .WithMany(p => p.AbcStationsSelectStationT)
                    .HasForeignKey(d => d.SelectStationTid)
                    .HasConstraintName("FK_SELSTATION_TID_STATIONS");

                entity.HasOne(d => d.TankFeedbackT)
                    .WithMany(p => p.AbcStationsTankFeedbackT)
                    .HasForeignKey(d => d.TankFeedbackTid)
                    .HasConstraintName("FK_TANK_FB_STATIONS");

                entity.HasOne(d => d.TankPreselectNumT)
                    .WithMany(p => p.AbcStationsTankPreselectNumT)
                    .HasForeignKey(d => d.TankPreselectNumTid)
                    .HasConstraintName("FK_TANK_PRESELNUM_TID_STATIONS");

                entity.HasOne(d => d.TankSelectNumT)
                    .WithMany(p => p.AbcStationsTankSelectNumT)
                    .HasForeignKey(d => d.TankSelectNumTid)
                    .HasConstraintName("FK_TANK_SEL_NUM_TID_STATIONS");

                entity.HasOne(d => d.TotalFlowControlT)
                    .WithMany(p => p.AbcStationsTotalFlowControlT)
                    .HasForeignKey(d => d.TotalFlowControlTid)
                    .HasConstraintName("FK_TOTFLOW_TID_STATIONS");

                entity.HasOne(d => d.TotalStationVolT)
                    .WithMany(p => p.AbcStationsTotalStationVolT)
                    .HasForeignKey(d => d.TotalStationVolTid)
                    .HasConstraintName("FK_TOTSTATION_TID_STATIONS");

                entity.HasOne(d => d.VolUom)
                    .WithMany(p => p.AbcStationsVolUom)
                    .HasForeignKey(d => d.VolUomId)
                    .HasConstraintName("FK_VUOM_ID_STONS");

                entity.HasOne(d => d.WildFlagT)
                    .WithMany(p => p.AbcStationsWildFlagT)
                    .HasForeignKey(d => d.WildFlagTid)
                    .HasConstraintName("FK_WILD_FLAG_TID_STATIONS");
            });

            modelBuilder.Entity<AbcSwingCriteria>(entity =>
            {
                entity.ToTable("ABC_SWING_CRITERIA");

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_SWING_CRITERIA")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Alias)
                    .IsRequired()
                    .HasColumnName("ALIAS")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");
            });

            modelBuilder.Entity<AbcSwingStates>(entity =>
            {
                entity.HasKey(e => e.Name)
                    .HasName("PK_SWING_STATES");

                entity.ToTable("ABC_SWING_STATES");

                entity.HasIndex(e => e.Alias)
                    .HasName("UQ_ALIAS_SWING_STATES")
                    .IsUnique();

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.Alias)
                    .IsRequired()
                    .HasColumnName("ALIAS")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");
            });

            modelBuilder.Entity<AbcTags>(entity =>
            {
                entity.ToTable("ABC_TAGS");

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_TAGS")
                    .IsUnique();

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.HasIndex(e => new { e.SystemId, e.BlockType, e.Name, e.Attribute })
                    .HasName("UQ_TAGS")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Attribute)
                    .IsRequired()
                    .HasColumnName("ATTRIBUTE")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.BlockType)
                    .IsRequired()
                    .HasColumnName("BLOCK_TYPE")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(72)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(64)
                    .IsUnicode(false);

                entity.Property(e => e.ReadEnabledFlag)
                    .IsRequired()
                    .HasColumnName("READ_ENABLED_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.Property(e => e.ReadString)
                    .HasColumnName("READ_STRING")
                    .HasMaxLength(132)
                    .IsUnicode(false);

                entity.Property(e => e.ReadValue).HasColumnName("READ_VALUE");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.ScanGroupId).HasColumnName("SCAN_GROUP_ID");

                entity.Property(e => e.SystemId).HasColumnName("SYSTEM_ID");

                entity.Property(e => e.ValueQuality)
                    .HasColumnName("VALUE_QUALITY")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.ValueTime)
                    .HasColumnName("VALUE_TIME")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.WriteNowFlag)
                    .IsRequired()
                    .HasColumnName("WRITE_NOW_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.WriteString)
                    .HasColumnName("WRITE_STRING")
                    .HasMaxLength(132)
                    .IsUnicode(false);

                entity.Property(e => e.WriteValue).HasColumnName("WRITE_VALUE");

                entity.HasOne(d => d.ScanGroup)
                    .WithMany(p => p.AbcTags)
                    .HasForeignKey(d => d.ScanGroupId)
                    .HasConstraintName("FK_SCAN_GROUP_ID_TAGS");
            });

            modelBuilder.Entity<AbcTankComposition>(entity =>
            {
                entity.HasKey(e => new { e.TankId, e.MatId })
                    .HasName("PK_TANK_COMP");

                entity.ToTable("ABC_TANK_COMPOSITION");

                entity.Property(e => e.TankId).HasColumnName("TANK_ID");

                entity.Property(e => e.MatId).HasColumnName("MAT_ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Fraction).HasColumnName("FRACTION");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.PrevFraction).HasColumnName("PREV_FRACTION");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.HasOne(d => d.Mat)
                    .WithMany(p => p.AbcTankComposition)
                    .HasForeignKey(d => d.MatId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MAT_ID_TANK_COMP");

                entity.HasOne(d => d.Tank)
                    .WithMany(p => p.AbcTankComposition)
                    .HasForeignKey(d => d.TankId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TANK_ID_TANK_COMP");
            });

            modelBuilder.Entity<AbcTankProps>(entity =>
            {
                entity.HasKey(e => new { e.TankId, e.PropId, e.SourceId })
                    .HasName("PK_TANK_PROPS");

                entity.ToTable("ABC_TANK_PROPS");

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.TankId).HasColumnName("TANK_ID");

                entity.Property(e => e.PropId).HasColumnName("PROP_ID");

                entity.Property(e => e.SourceId).HasColumnName("SOURCE_ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.GoodFlag)
                    .IsRequired()
                    .HasColumnName("GOOD_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.SelectedFlag)
                    .IsRequired()
                    .HasColumnName("SELECTED_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Value).HasColumnName("VALUE");

                entity.Property(e => e.ValueTime)
                    .HasColumnName("VALUE_TIME")
                    .HasColumnType("datetime2(0)");

                entity.HasOne(d => d.Prop)
                    .WithMany(p => p.AbcTankProps)
                    .HasForeignKey(d => d.PropId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PROP_ID_TANK_PROPS");

                entity.HasOne(d => d.Source)
                    .WithMany(p => p.AbcTankProps)
                    .HasForeignKey(d => d.SourceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SOURCE_ID_TANK_PROPS");

                entity.HasOne(d => d.Tank)
                    .WithMany(p => p.AbcTankProps)
                    .HasForeignKey(d => d.TankId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TANK_ID_TANK_PROPS");
            });

            modelBuilder.Entity<AbcTankStates>(entity =>
            {
                entity.HasKey(e => e.Name)
                    .HasName("PK_TANK_STATES");

                entity.ToTable("ABC_TANK_STATES");

                entity.HasIndex(e => e.Value)
                    .HasName("UQ_VALUE_TANK_STATES")
                    .IsUnique();

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Alias)
                    .IsRequired()
                    .HasColumnName("ALIAS")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(72)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.Value).HasColumnName("VALUE");
            });

            modelBuilder.Entity<AbcTanks>(entity =>
            {
                entity.ToTable("ABC_TANKS");

                entity.HasIndex(e => e.LimsTankName)
                    .HasName("SYS_C0012990")
                    .IsUnique();

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_TANKS")
                    .IsUnique();

                entity.HasIndex(e => e.PlanTankId)
                    .HasName("UQ_PLAN_TANK_TD_TANKS")
                    .IsUnique();

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.AbcServiceFlag)
                    .IsRequired()
                    .HasColumnName("ABC_SERVICE_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.AllowAutoSwing)
                    .IsRequired()
                    .HasColumnName("ALLOW_AUTO_SWING")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.AvailVolId).HasColumnName("AVAIL_VOL_ID");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.DcsServiceTid).HasColumnName("DCS_SERVICE_TID");

                entity.Property(e => e.DcsTankNum).HasColumnName("DCS_TANK_NUM");

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.FlowUomId).HasColumnName("FLOW_UOM_ID");

                entity.Property(e => e.InSerFlag)
                    .IsRequired()
                    .HasColumnName("IN_SER_FLAG")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LevelTid).HasColumnName("LEVEL_TID");

                entity.Property(e => e.LimsApprovalFlag)
                    .IsRequired()
                    .HasColumnName("LIMS_APPROVAL_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.Property(e => e.LimsEthanolFlag)
                    .IsRequired()
                    .HasColumnName("LIMS_ETHANOL_FLAG")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('NO')");

                entity.Property(e => e.LimsTankName)
                    .HasColumnName("LIMS_TANK_NAME")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.MatId).HasColumnName("MAT_ID");

                entity.Property(e => e.MaxLevelTid).HasColumnName("MAX_LEVEL_TID");

                entity.Property(e => e.MaxVolTid).HasColumnName("MAX_VOL_TID");

                entity.Property(e => e.MinLevelTid).HasColumnName("MIN_LEVEL_TID");

                entity.Property(e => e.MinVolTid).HasColumnName("MIN_VOL_TID");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.OrderId).HasColumnName("ORDER_ID");

                entity.Property(e => e.OrderSource)
                    .HasColumnName("ORDER_SOURCE")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.OutletVolTid).HasColumnName("OUTLET_VOL_TID");

                entity.Property(e => e.PlanTankId)
                    .HasColumnName("PLAN_TANK_ID")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.PrdgrpId).HasColumnName("PRDGRP_ID");

                entity.Property(e => e.PreVol).HasColumnName("PRE_VOL");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.RundnId).HasColumnName("RUNDN_ID");

                entity.Property(e => e.SampleBlendId).HasColumnName("SAMPLE_BLEND_ID");

                entity.Property(e => e.SampleName)
                    .HasColumnName("SAMPLE_NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.SampleStartDate)
                    .HasColumnName("SAMPLE_START_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.SampleStartVolume).HasColumnName("SAMPLE_START_VOLUME");

                entity.Property(e => e.SampleStopDate)
                    .HasColumnName("SAMPLE_STOP_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.SampleStopVolume).HasColumnName("SAMPLE_STOP_VOLUME");

                entity.Property(e => e.SharedName)
                    .HasColumnName("SHARED_NAME")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.SourceDestnType)
                    .IsRequired()
                    .HasColumnName("SOURCE_DESTN_TYPE")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.TqiDate)
                    .HasColumnName("TQI_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.TqiDone)
                    .IsRequired()
                    .HasColumnName("TQI_DONE")
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('YES')");

                entity.Property(e => e.VolUomId).HasColumnName("VOL_UOM_ID");

                entity.Property(e => e.Volume).HasColumnName("VOLUME");

                entity.HasOne(d => d.AvailVol)
                    .WithMany(p => p.AbcTanksAvailVol)
                    .HasForeignKey(d => d.AvailVolId)
                    .HasConstraintName("FK_AVAIL_VOL_ID_TANKS");

                entity.HasOne(d => d.DcsServiceT)
                    .WithMany(p => p.AbcTanksDcsServiceT)
                    .HasForeignKey(d => d.DcsServiceTid)
                    .HasConstraintName("FK_DCS_SERVICE_TID_TANKS");

                entity.HasOne(d => d.FlowUom)
                    .WithMany(p => p.AbcTanksFlowUom)
                    .HasForeignKey(d => d.FlowUomId)
                    .HasConstraintName("FK_FUOM_ID_TANKS");

                entity.HasOne(d => d.LevelT)
                    .WithMany(p => p.AbcTanksLevelT)
                    .HasForeignKey(d => d.LevelTid)
                    .HasConstraintName("FK_LEVEL_TANKS");

                entity.HasOne(d => d.Mat)
                    .WithMany(p => p.AbcTanks)
                    .HasForeignKey(d => d.MatId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MAT_ID_TANKS");

                entity.HasOne(d => d.MaxLevelT)
                    .WithMany(p => p.AbcTanksMaxLevelT)
                    .HasForeignKey(d => d.MaxLevelTid)
                    .HasConstraintName("FK_MAXLEVEL_TANKS");

                entity.HasOne(d => d.MaxVolT)
                    .WithMany(p => p.AbcTanksMaxVolT)
                    .HasForeignKey(d => d.MaxVolTid)
                    .HasConstraintName("FK_MAX_VOL_TID_TANKS");

                entity.HasOne(d => d.MinLevelT)
                    .WithMany(p => p.AbcTanksMinLevelT)
                    .HasForeignKey(d => d.MinLevelTid)
                    .HasConstraintName("FK_MINLEVEL_TANKS");

                entity.HasOne(d => d.MinVolT)
                    .WithMany(p => p.AbcTanksMinVolT)
                    .HasForeignKey(d => d.MinVolTid)
                    .HasConstraintName("FK_MIN_VOL_TID_TANKS");

                entity.HasOne(d => d.OutletVolT)
                    .WithMany(p => p.AbcTanksOutletVolT)
                    .HasForeignKey(d => d.OutletVolTid)
                    .HasConstraintName("FK_OUTLETVOL_TANKS");

                entity.HasOne(d => d.Rundn)
                    .WithMany(p => p.AbcTanksRundn)
                    .HasForeignKey(d => d.RundnId)
                    .HasConstraintName("FK_RUNDN_ID_TANKS");

                entity.HasOne(d => d.SampleBlend)
                    .WithMany(p => p.AbcTanks)
                    .HasForeignKey(d => d.SampleBlendId)
                    .HasConstraintName("FK_SAMPLE_BLEND_TANKS");

                entity.HasOne(d => d.VolUom)
                    .WithMany(p => p.AbcTanksVolUom)
                    .HasForeignKey(d => d.VolUomId)
                    .HasConstraintName("FK_VUOM_ID_TANKS");
            });

            modelBuilder.Entity<AbcTranstxt>(entity =>
            {
                entity.HasKey(e => new { e.Value, e.WordSet });

                entity.ToTable("ABC_TRANSTXT");

                entity.Property(e => e.Value)
                    .HasColumnName("VALUE")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.WordSet)
                    .HasColumnName("WORD_SET")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.UserValue)
                    .IsRequired()
                    .HasColumnName("USER_VALUE")
                    .HasMaxLength(20)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<AbcUnitConversion>(entity =>
            {
                entity.HasKey(e => new { e.FromUnit, e.ToUnit })
                    .HasName("PK_UNIT_CONVERSION");

                entity.ToTable("ABC_UNIT_CONVERSION");

                entity.Property(e => e.FromUnit).HasColumnName("FROM_UNIT");

                entity.Property(e => e.ToUnit).HasColumnName("TO_UNIT");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Factor)
                    .HasColumnName("FACTOR")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.FunctionName)
                    .HasColumnName("FUNCTION_NAME")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.HasOne(d => d.FromUnitNavigation)
                    .WithMany(p => p.AbcUnitConversionFromUnitNavigation)
                    .HasForeignKey(d => d.FromUnit)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FROM_UNIT_UNIT_CONVERSION");

                entity.HasOne(d => d.ToUnitNavigation)
                    .WithMany(p => p.AbcUnitConversionToUnitNavigation)
                    .HasForeignKey(d => d.ToUnit)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TO_UNIT_UNIT_CONVERSION");
            });

            modelBuilder.Entity<AbcUom>(entity =>
            {
                entity.ToTable("ABC_UOM");

                entity.HasIndex(e => e.Alias)
                    .HasName("UQ_ALIAS_UOM")
                    .IsUnique();

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.HasIndex(e => e.UnitsName)
                    .HasName("UQ_UNITS_NAME_UOM")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Alias)
                    .IsRequired()
                    .HasColumnName("ALIAS")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Dimension)
                    .IsRequired()
                    .HasColumnName("DIMENSION")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Nationality)
                    .HasColumnName("NATIONALITY")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.UnitsName)
                    .IsRequired()
                    .HasColumnName("UNITS_NAME")
                    .HasMaxLength(10)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<AbcUsages>(entity =>
            {
                entity.ToTable("ABC_USAGES");

                entity.HasIndex(e => e.Alias)
                    .HasName("UQ_ALIAS_USAGES")
                    .IsUnique();

                entity.HasIndex(e => e.Name)
                    .HasName("UQ_NAME_USAGES")
                    .IsUnique();

                entity.HasIndex(e => e.Rowid)
                    .HasName("ROWID$INDEX")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Alias)
                    .IsRequired()
                    .HasColumnName("ALIAS")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.LastUpdatedBy)
                    .HasColumnName("LAST_UPDATED_BY")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.LastUpdatedDate)
                    .HasColumnName("LAST_UPDATED_DATE")
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("NAME")
                    .HasMaxLength(12)
                    .IsUnicode(false);

                entity.Property(e => e.Rowid)
                    .HasColumnName("ROWID")
                    .HasDefaultValueSql("(newid())");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
