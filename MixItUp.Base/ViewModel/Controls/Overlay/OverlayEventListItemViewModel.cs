﻿using MixItUp.Base.Model.Overlay;
using MixItUp.Base.Util;
using System.Collections.Generic;

namespace MixItUp.Base.ViewModel.Controls.Overlay
{
    public class OverlayEventListItemViewModel : OverlayListItemViewModelBase
    {
        public bool ShowFollows
        {
            get { return this.eventListTypes.Contains(OverlayEventListItemTypeEnum.Followers); }
            set
            {
                this.UpdateEventListItem(OverlayEventListItemTypeEnum.Followers, value);
                this.NotifyPropertyChanged();
            }
        }

        public bool ShowHosts
        {
            get { return this.eventListTypes.Contains(OverlayEventListItemTypeEnum.Hosts); }
            set
            {
                this.UpdateEventListItem(OverlayEventListItemTypeEnum.Hosts, value);
                this.NotifyPropertyChanged();
            }
        }

        public bool ShowSubscribers
        {
            get { return this.eventListTypes.Contains(OverlayEventListItemTypeEnum.Subscribers); }
            set
            {
                this.UpdateEventListItem(OverlayEventListItemTypeEnum.Subscribers, value);
                this.NotifyPropertyChanged();
            }
        }

        public bool ShowDonations
        {
            get { return this.eventListTypes.Contains(OverlayEventListItemTypeEnum.Donations); }
            set
            {
                this.UpdateEventListItem(OverlayEventListItemTypeEnum.Donations, value);
                this.NotifyPropertyChanged();
            }
        }

        public bool ShowSparks
        {
            get { return this.eventListTypes.Contains(OverlayEventListItemTypeEnum.Sparks); }
            set
            {
                this.UpdateEventListItem(OverlayEventListItemTypeEnum.Sparks, value);
                this.NotifyPropertyChanged();
            }
        }

        public bool ShowEmbers
        {
            get { return this.eventListTypes.Contains(OverlayEventListItemTypeEnum.Embers); }
            set
            {
                this.UpdateEventListItem(OverlayEventListItemTypeEnum.Embers, value);
                this.NotifyPropertyChanged();
            }
        }

        public bool ShowMilestones
        {
            get { return this.eventListTypes.Contains(OverlayEventListItemTypeEnum.Milestones); }
            set
            {
                this.UpdateEventListItem(OverlayEventListItemTypeEnum.Milestones, value);
                this.NotifyPropertyChanged();
            }
        }

        private HashSet<OverlayEventListItemTypeEnum> eventListTypes = new HashSet<OverlayEventListItemTypeEnum>();

        public OverlayEventListItemViewModel()
            : base()
        {
            this.HTML = OverlayEventListItemModel.HTMLTemplate;
        }

        public OverlayEventListItemViewModel(OverlayEventListItemModel item)
            : base(item.TotalToShow, item.FadeOut, item.Width, item.Height, item.TextFont, item.TextColor, item.BorderColor, item.BackgroundColor, item.Effects.EntranceAnimation, item.Effects.ExitAnimation, item.HTML)
        { }

        public override OverlayItemModelBase GetOverlayItem()
        {
            if (this.Validate() && this.eventListTypes.Count > 0)
            {
                this.TextColor = ColorSchemes.GetColorCode(this.TextColor);
                this.BorderColor = ColorSchemes.GetColorCode(this.BorderColor);
                this.BackgroundColor = ColorSchemes.GetColorCode(this.BackgroundColor);

                return new OverlayEventListItemModel(this.HTML, this.eventListTypes, totalToShow, fadeOut, this.Font, this.width, this.height, this.BorderColor, this.BackgroundColor, this.TextColor, this.entranceAnimation, this.exitAnimation);
            }
            return null;
        }

        private void UpdateEventListItem(OverlayEventListItemTypeEnum type, bool value)
        {
            if (value)
            {
                this.eventListTypes.Add(type);
            }
            else
            {
                this.eventListTypes.Remove(type);
            }
        }
    }
}