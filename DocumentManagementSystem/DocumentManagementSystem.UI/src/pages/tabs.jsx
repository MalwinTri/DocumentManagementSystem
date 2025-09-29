import * as React from "react";
import * as TabsPrimitive from "@radix-ui/react-tabs";
import { cva } from "class-variance-authority";
import { cn } from "@/lib/utils";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "../components/ui/tabs.jsx";


export const Tabs = TabsPrimitive.Root;

export const TabsList = React.forwardRef(function TabsList(
    { className, ...props },
    ref
) {
    return (
        <TabsPrimitive.List
            ref={ref}
            className={cn(
                "inline-flex h-10 items-center justify-center rounded-xl bg-muted p-1 text-muted-foreground",
                className
            )}
            {...props}
        />
    );
});

const tabsTriggerVariants = cva(
    "inline-flex items-center justify-center whitespace-nowrap rounded-lg px-3 py-2 text-sm font-medium ring-offset-background transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 data-[state=active]:bg-background data-[state=active]:text-foreground"
);

export const TabsTrigger = React.forwardRef(function TabsTrigger(
    { className, ...props },
    ref
) {
    return (
        <TabsPrimitive.Trigger
            ref={ref}
            className={cn(tabsTriggerVariants(), className)}
            {...props}
        />
    );
});

export const TabsContent = React.forwardRef(function TabsContent(
    { className, ...props },
    ref
) {
    return (
        <TabsPrimitive.Content
            ref={ref}
            className={cn(
                "mt-2 ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
                className
            )}
            {...props}
        />
    );
});
