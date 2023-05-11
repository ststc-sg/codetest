function getNodePos(el)
{
    var body = d3.select('body').node();

    for (var lx = 0, ly = 0;
         el != null && el != body;
         lx += (el.offsetLeft || el.clientLeft), ly += (el.offsetTop || el.clientTop), el = (el.offsetParent || el.parentNode))
        ;
    return {x: lx, y: ly};
}
d3.contextMenu = function (menu,menuoffsetx,openCallback) {

	// create the div element that will hold the context menu
	d3.selectAll('.d3-context-menu').data([1])
		.enter()
		.append('div')
		.attr('class', 'd3-context-menu');

	// close menu
	d3.select('body').on('click.d3-context-menu', function() {
		d3.select('.d3-context-menu').style('display', 'none');
	});
	
	d3.select('.d3-context-menu').on('mouseleave', function(d, i) {
			d3.select('.d3-context-menu').style('display', 'none');
		});

	// this gets executed when a contextmenu event occurs
	return function(data, index) {	
		var elm = this;
		d3.selectAll('.d3-context-menu').html('');
		var list = d3.selectAll('.d3-context-menu').append('ul');
		list.selectAll('li').data(menu).enter()
			.append('li')
			.html(function(d) {
				return d.title;
			})
			.on('click', function(d, i) {
				d.action(elm, data, index);
				d3.select('.d3-context-menu').style('display', 'none');
			});

		// the openCallback allows an action to fire before the menu is displayed
		// an example usage would be closing a tooltip
		if (openCallback) openCallback(data, index);
		
		// display context menu,use global x position to dirty fix window in window offset
		d3.select('.d3-context-menu')
			.style('left', (d3.event.screenX+menuoffsetx) + 'px')
			.style('top', (d3.event.pageY - 2) + 'px')
			.style('display', 'block');

		d3.event.preventDefault();
	};
};