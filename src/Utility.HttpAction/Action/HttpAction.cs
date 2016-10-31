﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utility.HttpAction.Core;
using Utility.HttpAction.Event;
using Utility.HttpAction.Service;

namespace Utility.HttpAction.Action
{
    public abstract class HttpAction : IHttpAction
    {
        protected virtual int MaxReTryTimes { get; set; } = 3;

        protected int ExcuteTimes { get; set; }
        protected int RetryTimes { get; set; }
        protected IHttpService HttpService { get; set; }

        protected HttpAction(IHttpService httpHttpService)
        {
            HttpService = httpHttpService;
        }

        protected virtual ActionEvent NotifyActionEvent(ActionEvent actionEvent)
        {
            OnActionEvent?.Invoke(this, actionEvent);
            return actionEvent;
        }

        protected virtual ActionEvent NotifyActionEvent(ActionEventType type, object target = null)
        {
            return NotifyActionEvent(target == null ? ActionEvent.EmptyEvents[type] : ActionEvent.CreateEvent(type, target));
        }

        public abstract HttpRequestItem BuildRequest();

        public abstract ActionEvent HandleResponse(HttpResponseItem response);

        public event ActionEventListener OnActionEvent;

        public virtual ActionEvent HandleException(Exception ex)
        {
            if (RetryTimes < MaxReTryTimes)
            {
                return NotifyActionEvent(ActionEvent.CreateEvent(ActionEventType.EvtRetry, ex));
            }
            else
            {
                return NotifyActionEvent(ActionEvent.CreateEvent(ActionEventType.EvtError, ex));
            }
        }

        public virtual async Task<ActionEvent> ExecuteAsync(CancellationToken token)
        {
            ++ExcuteTimes;
            if (token.IsCancellationRequested)
            {
                return NotifyActionEvent(ActionEvent.CreateEvent(ActionEventType.EvtCanceled, this));
            }
            try
            {
                var requestItem = BuildRequest();
                var response = await HttpService.ExecuteHttpRequestAsync(requestItem, token);
                return HandleResponse(response);
            }
            catch (TaskCanceledException)
            {
                return NotifyActionEvent(ActionEvent.CreateEvent(ActionEventType.EvtCanceled, this));
            }
            catch (OperationCanceledException)
            {
                return NotifyActionEvent(ActionEvent.CreateEvent(ActionEventType.EvtCanceled, this));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
    }
}