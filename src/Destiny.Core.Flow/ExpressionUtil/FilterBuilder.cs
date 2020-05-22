﻿using Destiny.Core.Flow.Enums;
using Destiny.Core.Flow.Exceptions;
using Destiny.Core.Flow.Extensions;
using Destiny.Core.Flow.Filter;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Destiny.Core.Flow.ExpressionUtil
{
    public static class FilterBuilder
    {
        public static Expression<Func<T, bool>> GetExpression<T>(QueryFilter filterItem)
        {
            filterItem.NotNull("filterItem");
            ParameterExpression param = Expression.Parameter(typeof(T), "m");

            Expression expression = GetExpressionBody(param, filterItem);
            return Expression.Lambda<Func<T, bool>>(expression, param);
        }

        private static Expression GetExpressionBody(ParameterExpression param, QueryFilter filterItem)
        {
    
            List<Expression> expressions = new List<Expression>();
            Expression expression = Expression.Constant(true);

            foreach (var item in filterItem.Conditions)
            {
                expressions.Add(GetExpressionBody(param, item));
            }
            foreach (var item in filterItem.Filters)
            {
                expressions.Add(GetExpressionBody(param, item));
            }

            if (filterItem.FilterConnect == FilterConnect.And)
            {
                return expressions.Aggregate(Expression.AndAlso);
            }
            else
            {
                return expressions.Aggregate(Expression.OrElse);
            }
        }

        private static Expression GetExpressionBody(ParameterExpression param, FilterCondition filter)
        {

            var lambda = GetPropertyLambdaExpression(param, filter);
            var constant = ChangeTypeToExpression(filter, lambda.Body.Type);

            return GetOperateExpression(filter.Operator, lambda.Body, constant);

        }


        /// <summary>
        /// 得到值
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="conversionType"></param>
        /// <returns></returns>

        private static Expression ChangeTypeToExpression(FilterCondition filter, Type conversionType)
        {
            var constant = Expression.Constant(true);

            var value = filter.Value.AsTo(conversionType);
            if ((filter.Value?.ToString().IsNullOrWhiteSpace() ?? false) ||(value.ToString()?.IsNullOrWhiteSpace() ?? false))
            {
                return constant;
            }

            return Expression.Constant(value, conversionType);
        }
        private static Expression GetOperateExpression(FilterOperator operate, Expression member, Expression expression2)
        {
            switch (operate)
            {
                case FilterOperator.Equal:
                    return Expression.Equal(member, expression2);

                case FilterOperator.NotEqual:
                    return Expression.NotEqual(member, expression2);

                case FilterOperator.GreaterThan:
                    return Expression.GreaterThan(member, expression2);

                case FilterOperator.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(member, expression2);

                case FilterOperator.LessThan:
                    return Expression.LessThan(member, expression2);

                case FilterOperator.LessThanOrEqual:
                    return Expression.LessThanOrEqual(member, expression2);

                case FilterOperator.Like:

                    return Like(member, expression2);
                default:
                    throw new AppException($"此{operate}过滤条件不存在！！！");

            }
        }


        private static Expression Like(Expression member, Expression expression2)
        {
            if (expression2.Type != typeof(string))
            {
                throw new NotSupportedException("“Like”比较方式只支持字符串类型的数据");
            }
            var functions = Expression.Property(null, typeof(EF).GetProperty(nameof(EF.Functions)));
            var like = typeof(DbFunctionsExtensions).GetMethod(nameof(DbFunctionsExtensions.Like), new Type[] { functions.Type, typeof(string), typeof(string) });
            var methodCallExpression = Expression.Call(
             null,
             like,
             functions,
             member,
             expression2);
            return methodCallExpression;
        }
        private static LambdaExpression GetPropertyLambdaExpression(ParameterExpression parameter, FilterCondition filter)
        {
            var type = parameter.Type;
            var property = type.GetProperty(filter.Field);
            if (property == null)
            {
                throw new AppException($"没有得到{filter.Field}该名字!!!");
            }
            Expression propertyAccess = Expression.MakeMemberAccess(parameter, property);
            return Expression.Lambda(propertyAccess, parameter);
        }
    }
}
